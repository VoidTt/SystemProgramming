using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;

namespace Homework;

public class MainForm : Form
{
    private readonly TextBox source = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
    private readonly TextBox result = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Dock = DockStyle.Fill };
    private readonly Button run = new() { Text = "Анализировать", Width = 140 };
    private readonly Button stop = new() { Text = "Остановить", Width = 120, Enabled = false };
    private readonly CheckBox toFile = new() { Text = "Сохранить отчёт в файл", AutoSize = true };
    private readonly CheckBox[] flags = new[] { "Предложения", "Символы", "Слова", "Вопросительные", "Восклицательные" }.Select(s => new CheckBox { Text = s, Checked = true, AutoSize = true }).ToArray();
    private CancellationTokenSource? cts;
    private static readonly int Variant = 1;

    public MainForm()
    {
        Text = "Задание " + Variant + " — анализ текста";
        Width = 850; Height = 650; StartPosition = FormStartPosition.CenterScreen;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = Variant == 3 ? 98 : 50, Padding = new Padding(8), AutoScroll = true };
        top.Controls.Add(run);
        if (Variant >= 2) top.Controls.Add(stop);
        top.Controls.Add(toFile);
        if (Variant == 3) foreach (var f in flags) top.Controls.Add(f);
        var split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(800, 520), Orientation = Orientation.Horizontal, SplitterDistance = 270 };
        split.Panel1.Controls.Add(source);
        split.Panel2.Controls.Add(result);
        Controls.Add(split); Controls.Add(top);
        run.Click += async (_, _) => await AnalyzeAsync();
        stop.Click += (_, _) => cts?.Cancel();
        FormClosing += (_, _) => cts?.Cancel();
    }

    private async Task AnalyzeAsync()
    {
        cts?.Dispose();
        cts = new CancellationTokenSource();
        var token = cts.Token;
        run.Enabled = false; stop.Enabled = Variant >= 2;
        var selected = flags.Select(f => f.Checked).ToArray();
        string input = source.Text;
        bool save = toFile.Checked;
        try
        {
            var report = await Task.Run(() => BuildReport(input, selected, token), token);
            token.ThrowIfCancellationRequested();
            if (IsDisposed) return;
            result.Text = report;
            if (save)
            {
                using var dialog = new SaveFileDialog { Filter = "Текстовые файлы|*.txt", FileName = "report.txt" };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    await File.WriteAllTextAsync(dialog.FileName, report, Encoding.UTF8, token);
                    result.AppendText(Environment.NewLine + "Отчёт сохранён: " + dialog.FileName);
                }
            }
        }
        catch (OperationCanceledException) { if (!IsDisposed) result.Text = "Анализ остановлен."; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { if (!IsDisposed) { run.Enabled = true; stop.Enabled = false; } }
    }

    private static string BuildReport(string input, bool[] selected, CancellationToken token)
    {
        var sentences = 0; var questions = 0; var exclamations = 0;
        bool hasText = false;
        for (int i = 0; i < input.Length; i++)
        {
            if ((i & 4095) == 0) token.ThrowIfCancellationRequested();
            char c = input[i];
            if (c is '.' or '!' or '?')
            {
                if (hasText)
                {
                    sentences++;
                    int j = i;
                    bool q = false, ex = false;
                    while (j < input.Length && input[j] is '.' or '!' or '?')
                    { q |= input[j] == '?'; ex |= input[j] == '!'; j++; }
                    if (q) questions++;
                    if (ex) exclamations++;
                    i = j - 1;
                    hasText = false;
                }
            }
            else if (!char.IsWhiteSpace(c)) hasText = true;
        }
        if (hasText) sentences++;
        token.ThrowIfCancellationRequested();
        int words = 0; bool inWord = false;
        foreach (char c in input)
        {
            token.ThrowIfCancellationRequested();
            bool wordChar = char.IsLetterOrDigit(c);
            if (wordChar && !inWord) words++;
            inWord = wordChar;
        }
        var lines = new List<string> { "ОТЧЁТ", "" };
        if (Variant != 3 || selected[0]) lines.Add($"Предложений: {sentences}");
        if (Variant != 3 || selected[1]) lines.Add($"Символов: {input.Length}");
        if (Variant != 3 || selected[2]) lines.Add($"Слов: {words}");
        if (Variant != 3 || selected[3]) lines.Add($"Вопросительных предложений: {questions}");
        if (Variant != 3 || selected[4]) lines.Add($"Восклицательных предложений: {exclamations}");
        return string.Join(Environment.NewLine, lines);
    }
}
