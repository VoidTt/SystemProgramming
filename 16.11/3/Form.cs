using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
namespace Homework;

public class MainForm : Form
{
    private readonly int variant = 3;
    private readonly TextBox path = new() { Width = 630 };
    private readonly TextBox word = new() { Width = 630 };
    private readonly Button start = new() { Text = "Старт", AutoSize = true };
    private readonly Button browse = new() { Text = "Обзор", AutoSize = true };
    private readonly TextBox output = new() { Width = 630, Height = 390, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false };
    private CancellationTokenSource? cancellation;

    public MainForm()
    {
        Text = variant == 3 ? "Числа Фибоначчи" : variant == 4 ? "Поиск в файле" : "Поиск в папке";
        ClientSize = new Size(680, 620);
        StartPosition = FormStartPosition.CenterScreen;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12) };
        Controls.Add(panel);
        panel.Controls.Add(new Label { Text = variant == 3 ? "Верхняя граница (включительно)" : "Путь к файлу или папке", AutoSize = true });
        panel.Controls.Add(path);
        if (variant == 3) path.Text = "1000";
        else
        {
            panel.Controls.Add(browse);
            panel.Controls.Add(new Label { Text = "Искомое слово (без учёта регистра)", AutoSize = true });
            panel.Controls.Add(word);
        }
        panel.Controls.Add(start); panel.Controls.Add(output);
        browse.Click += (_, _) => Browse();
        start.Click += async (_, _) => await RunAsync();
        FormClosing += (_, _) => cancellation?.Cancel();
    }

    private void Browse()
    {
        if (variant == 4)
        {
            using var dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK) path.Text = dialog.FileName;
        }
        else
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog(this) == DialogResult.OK) path.Text = dialog.SelectedPath;
        }
    }

    private async Task RunAsync()
    {
        string location = path.Text.Trim(), search = word.Text.Trim();
        BigInteger limit = 0;
        if (variant == 3 && (!BigInteger.TryParse(location, out limit) || limit < 0))
        { MessageBox.Show("Введите неотрицательное целое число."); return; }
        if (variant != 3 && (search.Length == 0 || !(variant == 4 ? File.Exists(location) : Directory.Exists(location))))
        { MessageBox.Show("Укажите существующий путь и слово."); return; }
        cancellation?.Dispose(); cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        start.Enabled = false; output.Text = "Выполняется...";
        try
        {
            string report = await Task.Run(async () => {
                if (variant == 3) return Fibonacci(limit, token);
                if (variant == 4) return await SearchFile(location, search, token);
                var report = new StringBuilder();
                var folders = new Stack<string>(); folders.Push(location);
                while (folders.Count > 0)
                {
                    token.ThrowIfCancellationRequested();
                    string folder = folders.Pop();
                    try
                    {
                        foreach (string sub in Directory.GetDirectories(folder))
                            if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) == 0) folders.Push(sub);
                        foreach (string file in Directory.GetFiles(folder))
                        {
                            try { report.AppendLine(await SearchFile(file, search, token)); }
                            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                            { report.AppendLine($"Пропущен {file}: {ex.Message}"); }
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    { report.AppendLine($"Нет доступа к {folder}: {ex.Message}"); }
                }
                return report.Length == 0 ? "Файлы не найдены." : report.ToString();
            }, token);
            if (!IsDisposed) output.Text = report;
        }
        catch (OperationCanceledException) { if (!IsDisposed) output.Text = "Остановлено."; }
        catch (Exception ex) { if (!IsDisposed) output.Text = "Ошибка: " + ex.Message; }
        finally { if (!IsDisposed) start.Enabled = true; }
    }

    public static string Fibonacci(BigInteger limit, CancellationToken token)
    {
        var report = new StringBuilder();
        BigInteger a = 0, b = 1;
        int count = 0;
        while (a <= limit)
        {
            token.ThrowIfCancellationRequested();
            report.AppendLine(a.ToString()); count++;
            (a, b) = (b, a + b);
        }
        report.AppendLine("Количество: " + count);
        return report.ToString();
    }

    public static async Task<string> SearchFile(string file, string word, CancellationToken token)
    {
        string text = await File.ReadAllTextAsync(file, token);
        // Ищем целое слово: «кот» не засчитывается внутри «котёнок».
        string pattern = @"(?<![\p{L}\p{N}_])" + Regex.Escape(word) + @"(?![\p{L}\p{N}_])";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        int count = 0;
        foreach (Match match in regex.Matches(text)) { token.ThrowIfCancellationRequested(); count++; }
        return $"Название файла: {Path.GetFileName(file)}\r\nПуть к файлу: {Path.GetFullPath(file)}\r\nКоличество вхождений слова: {count}\r\n";
    }
}

