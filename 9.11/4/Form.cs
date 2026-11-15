using System;
using System.Drawing;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Homework;

public class MainForm : Form
{
    private static readonly int TaskNumber = 4;
    private readonly TextBox lowerBox = new() { Text = "2", Width = 95 };
    private readonly TextBox upperBox = new() { Width = 95 };
    private readonly ListBox primesList = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    private readonly ListBox fibonacciList = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    private readonly Label primeStatus = new() { Text = "Не запущен", AutoSize = true };
    private readonly Label fibStatus = new() { Text = "Не запущен", AutoSize = true };
    private readonly Button startButton = new() { Text = "Запустить", AutoSize = true };
    private readonly Button restartButton = new() { Text = "Рестарт с новыми границами", AutoSize = true };
    private readonly Button stopPrimesButton = new() { Text = "Остановить простые", AutoSize = true };
    private readonly Button stopFibButton = new() { Text = "Остановить Фибоначчи", AutoSize = true };
    private readonly Button pausePrimesButton = new() { Text = "Пауза простых", AutoSize = true };
    private readonly Button resumePrimesButton = new() { Text = "Продолжить простые", AutoSize = true };
    private readonly Button pauseFibButton = new() { Text = "Пауза Фибоначчи", AutoSize = true };
    private readonly Button resumeFibButton = new() { Text = "Продолжить Фибоначчи", AutoSize = true };
    private readonly TextBox fibLower = new() { Width = 95 };
    private readonly TextBox fibUpper = new() { Width = 95 };
    private readonly System.Windows.Forms.Timer stateTimer = new() { Interval = 100 };
    private Worker? primeWorker;
    private Worker? fibWorker;
    private bool closing;
    private bool busy;

    public MainForm()
    {
        Text = $"Модуль 3 — Задание {TaskNumber}";
        Size = new Size(1050, 630);
        MinimumSize = new Size(800, 450);
        StartPosition = FormStartPosition.CenterScreen;

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(main);

        var bounds = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        bounds.Controls.Add(new Label { Text = "Простые: от (пусто = 2):", AutoSize = true, Margin = new Padding(4, 9, 4, 0) });
        bounds.Controls.Add(lowerBox);
        bounds.Controls.Add(new Label { Text = "До (пусто = без ограничения):", AutoSize = true, Margin = new Padding(12, 9, 4, 0) });
        bounds.Controls.Add(upperBox);
        if (TaskNumber >= 5)
        {
            bounds.Controls.Add(new Label { Text = "Фибоначчи: от (пусто = 0)", AutoSize = true });
            bounds.Controls.Add(fibLower);
            bounds.Controls.Add(new Label { Text = "До (пусто = без ограничения)", AutoSize = true });
            bounds.Controls.Add(fibUpper);
        }
        main.Controls.Add(bounds, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 8, 0, 8) };
        actions.Controls.Add(startButton);
        if (TaskNumber >= 3) { actions.Controls.Add(stopPrimesButton); actions.Controls.Add(stopFibButton); }
        if (TaskNumber >= 4)
        {
            actions.Controls.Add(pausePrimesButton); actions.Controls.Add(resumePrimesButton);
            actions.Controls.Add(pauseFibButton); actions.Controls.Add(resumeFibButton);
        }
        if (TaskNumber >= 5) actions.Controls.Add(restartButton);
        main.Controls.Add(actions, 0, 1);

        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = TaskNumber >= 2 ? 2 : 1, RowCount = 1 };
        if (TaskNumber >= 2)
        {
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        }
        columns.Controls.Add(MakeGroup("Простые числа", primesList, primeStatus), 0, 0);
        if (TaskNumber >= 2) columns.Controls.Add(MakeGroup("Числа Фибоначчи", fibonacciList, fibStatus), 1, 0);
        main.Controls.Add(columns, 0, 2);

        startButton.Click += async (_, _) => await StartAsync(false);
        restartButton.Click += async (_, _) => await StartAsync(true);
        stopPrimesButton.Click += (_, _) => Stop(primeWorker);
        stopFibButton.Click += (_, _) => Stop(fibWorker);
        pausePrimesButton.Click += (_, _) => Pause(primeWorker, primeStatus);
        pauseFibButton.Click += (_, _) => Pause(fibWorker, fibStatus);
        resumePrimesButton.Click += (_, _) => Resume(primeWorker, primeStatus);
        resumeFibButton.Click += (_, _) => Resume(fibWorker, fibStatus);
        stateTimer.Tick += (_, _) => startButton.Enabled = !busy && primeWorker?.IsAlive != true && fibWorker?.IsAlive != true;
        stateTimer.Start();
        FormClosing += (_, _) =>
        {
            closing = true;
            stateTimer.Stop(); stateTimer.Dispose();
            Stop(primeWorker);
            Stop(fibWorker);
            primeWorker?.Join();
            fibWorker?.Join();
        };
    }

    private static Control MakeGroup(string title, ListBox list, Label status)
    {
        var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(10) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(list, 0, 0);
        layout.Controls.Add(status, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private bool TryBounds(out long low, out long? high)
    {
        low = 2;
        high = null;
        if (!string.IsNullOrWhiteSpace(lowerBox.Text) &&
            !long.TryParse(lowerBox.Text, out low))
        {
            MessageBox.Show("Нижняя граница должна быть целым числом.");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(upperBox.Text))
        {
            if (!long.TryParse(upperBox.Text, out long parsed) || parsed < low)
            {
                MessageBox.Show("Верхняя граница должна быть целым числом не меньше нижней.");
                return false;
            }
            high = parsed;
        }
        return true;
    }

    private async Task StartAsync(bool restart)
    {
        if (busy || closing || !TryBounds(out long low, out long? high)) return;
        BigInteger fibMin = 0;
        BigInteger? fibMax = null;
        if (TaskNumber >= 5)
        {
            if (!string.IsNullOrWhiteSpace(fibLower.Text) && !BigInteger.TryParse(fibLower.Text, out fibMin))
            { MessageBox.Show("Некорректная нижняя граница Фибоначчи."); return; }
            if (!string.IsNullOrWhiteSpace(fibUpper.Text))
            {
                if (!BigInteger.TryParse(fibUpper.Text, out var end) || end < fibMin)
                { MessageBox.Show("Некорректная верхняя граница Фибоначчи."); return; }
                fibMax = end;
            }
        }
        if (!restart && (primeWorker?.IsAlive == true || fibWorker?.IsAlive == true)) return;
        busy = true;
        startButton.Enabled = false;
        restartButton.Enabled = false;
        try
        {
            if (primeWorker != null || fibWorker != null)
            {
                Stop(primeWorker);
                Stop(fibWorker);
                var oldPrime = primeWorker;
                var oldFib = fibWorker;
                await Task.Run(() => { oldPrime?.Join(); oldFib?.Join(); });
                if (closing) return;
                primeWorker = null;
                fibWorker = null;
            }
            primesList.Items.Clear();
            fibonacciList.Items.Clear();
            primeStatus.Text = "Работает";
            primeWorker = new Worker(token => GeneratePrimes(low, high, token));
            primeWorker.Start();
            if (TaskNumber >= 2)
            {
                fibStatus.Text = "Работает";
                fibWorker = new Worker(token => GenerateFibonacci(fibMin, fibMax, token));
                fibWorker.Start();
            }
        }
        finally
        {
            busy = false;
            if (!closing) restartButton.Enabled = true;
        }
    }

    private void GeneratePrimes(long low, long? high, Worker worker)
    {
        try
        {
            for (BigInteger n = BigInteger.Max(2, low); !worker.Cancelled && (!high.HasValue || n <= high.Value); n++)
            {
                if (!worker.Wait()) return;
                if (IsPrime(n, worker)) AddValue(primesList, n.ToString());
                Thread.Sleep(8);
            }
        }
        finally { SetStatus(primeStatus, worker.Cancelled ? "Остановлен" : "Завершён"); }
    }

    private void GenerateFibonacci(BigInteger low, BigInteger? high, Worker worker)
    {
        BigInteger a = 0, b = 1;
        try
        {
            while (!worker.Cancelled && (!high.HasValue || a <= high.Value))
            {
                if (!worker.Wait()) return;
                if (a >= low) AddValue(fibonacciList, a.ToString());
                (a, b) = (b, a + b);
                if (worker.Cancelled) return;
                Thread.Sleep(80);
            }
        }
        finally { SetStatus(fibStatus, worker.Cancelled ? "Остановлен" : "Завершён"); }
    }

    private static bool IsPrime(BigInteger n, Worker worker)
    {
        if (n < 2) return false;
        if (n == 2) return true;
        if (n % 2 == 0) return false;
        for (BigInteger d = 3; d <= n / d; d += 2)
            {
                if (!worker.Wait()) return false;
                if (n % d == 0) return false;
            }
        return true;
    }

    private void AddValue(ListBox list, string value)
    {
        if (closing || !IsHandleCreated) return;
        try
        {
            BeginInvoke((Action)(() =>
            {
                if (closing || IsDisposed) return;
                list.Items.Add(value);
                if (list.Items.Count > 1000) list.Items.RemoveAt(0);
                list.TopIndex = list.Items.Count - 1;
            }));
        }
        catch (InvalidOperationException) { }
    }

    private void SetStatus(Label label, string status)
    {
        if (closing || !IsHandleCreated) return;
        try { BeginInvoke((Action)(() => { if (!closing && !IsDisposed) label.Text = status; })); }
        catch (InvalidOperationException) { }
    }

    private static void Stop(Worker? worker) => worker?.Stop();
    private static void Pause(Worker? worker, Label status)
    {
        if (worker == null || worker.Cancelled || !worker.IsAlive) return;
        worker.Pause();
        status.Text = "Пауза";
    }
    private static void Resume(Worker? worker, Label status)
    {
        if (worker == null || worker.Cancelled || !worker.IsAlive) return;
        worker.Resume();
        status.Text = "Работает";
    }

    private class Worker
    {
        private readonly ManualResetEventSlim gate = new(true);
        private readonly Thread thread;
        private volatile bool cancelled;
        public bool Cancelled => cancelled;
        public bool IsAlive => thread.IsAlive;
        public Worker(Action<Worker> action)
        {
            thread = new Thread(() => action(this)) { IsBackground = true };
        }
        public void Start() => thread.Start();
        public void Stop() { cancelled = true; gate.Set(); }
        public void Pause() => gate.Reset();
        public void Resume() => gate.Set();
        public bool Wait()
        {
            gate.Wait();
            return !cancelled;
        }
        public void Join() { if (thread.IsAlive) thread.Join(); }
    }
}
