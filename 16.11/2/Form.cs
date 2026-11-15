using System.Collections.Concurrent;
using System.Diagnostics;
namespace Homework;

public class MainForm : Form
{
    private readonly bool race = true;
    private readonly NumericUpDown count = new() { Minimum = 1, Maximum = 40, Value = 6 };
    private readonly Button start = new() { Text = "Старт", AutoSize = true };
    private readonly FlowLayoutPanel panel = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly TextBox result = new() { Multiline = true, ReadOnly = true, Width = 620, Height = 120 };
    private readonly List<Thread> threads = new();
    private readonly ConcurrentQueue<(int index, int value)> updates = new();
    private readonly ConcurrentQueue<(int horse, long time)> finishes = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 40 };
    private ColoredBar[] bars = [];
    private CancellationTokenSource? cancellation;
    private bool running;

    public MainForm()
    {
        Text = race ? "Конные скачки" : "Танцующие прогресс-бары";
        ClientSize = new Size(680, 600);
        StartPosition = FormStartPosition.CenterScreen;
        panel.Padding = new Padding(12);
        Controls.Add(panel);
        if (!race) { panel.Controls.Add(new Label { Text = "Количество полос:", AutoSize = true }); panel.Controls.Add(count); }
        panel.Controls.Add(start);
        panel.Controls.Add(result);
        start.Click += (_, _) => StartGeneration();
        timer.Tick += (_, _) => RefreshBars();
        timer.Start();
    }

    private void StartGeneration()
    {
        if (running) return;
        foreach (var bar in bars) { panel.Controls.Remove(bar); bar.Dispose(); }
        threads.Clear(); updates.Clear(); finishes.Clear(); result.Clear();
        cancellation?.Dispose();
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        int total = race ? 5 : (int)count.Value;
        bars = new ColoredBar[total];
        running = true; start.Enabled = false;
        var clock = Stopwatch.StartNew();
        for (int i = 0; i < total; i++)
        {
            bars[i] = new ColoredBar {
                Width = 620, Height = 28,
                Text = race ? "Лошадь " + (i + 1) : "Полоса " + (i + 1),
                BarColor = Color.FromArgb(Random.Shared.Next(40,230),Random.Shared.Next(40,230),Random.Shared.Next(40,230))
            };
            panel.Controls.Add(bars[i]);
            int index = i;
            // Каждая полоса обслуживается отдельным Thread.
            var thread = new Thread(() => {
                int value = 0;
                while (value < 100)
                {
                    if (token.WaitHandle.WaitOne(Random.Shared.Next(30,150))) return;
                    value = Math.Min(100, value + Random.Shared.Next(1,9));
                    updates.Enqueue((index,value));
                }
                finishes.Enqueue((index + 1, clock.ElapsedMilliseconds));
            }) { IsBackground = true };
            threads.Add(thread);
            thread.Start();
        }
    }

    private void RefreshBars()
    {
        while (updates.TryDequeue(out var update)) bars[update.index].Value = update.value;
        if (!running || threads.Any(t => t.IsAlive)) return;
        // После завершения потоков дочитываем последние обновления.
        while (updates.TryDequeue(out var update)) bars[update.index].Value = update.value;
        running = false; start.Enabled = true;
        if (race)
        {
            var ordered = finishes.OrderBy(x => x.time).ThenBy(x => x.horse).ToArray();
            result.Text = "Место | Лошадь | Время (мс)" + Environment.NewLine;
            for (int i = 0; i < ordered.Length; i++)
                result.AppendText($"{i + 1} | {ordered[i].horse} | {ordered[i].time}{Environment.NewLine}");
        }
        else result.Text = "Все полосы заполнены.";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        cancellation?.Cancel();
        foreach (var thread in threads) thread.Join();
        timer.Stop(); timer.Dispose(); cancellation?.Dispose();
        base.OnFormClosed(e);
    }
}

// Обычный ProgressBar Windows игнорирует цвет при включённой теме,
// поэтому рисуем простую полосу сами.
public class ColoredBar : Control
{
    private int value;
    public Color BarColor { get; set; }
    public int Value { get => value; set { this.value = value; Invalidate(); } }
    public ColoredBar() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Gainsboro);
        using var brush = new SolidBrush(BarColor);
        e.Graphics.FillRectangle(brush, 0, 0, Width * value / 100, Height);
        e.Graphics.DrawRectangle(Pens.Gray, 0, 0, Width - 1, Height - 1);
        TextRenderer.DrawText(e.Graphics, $"{Text}: {value}%", Font, ClientRectangle, Color.Black);
    }
}

