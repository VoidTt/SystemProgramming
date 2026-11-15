using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Homework;

public class MainForm : Form
{
    private readonly TextBox source = new() { Width = 520 };
    private readonly TextBox destination = new() { Width = 520 };
    private readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, WordWrap = false };
    private readonly Button run = new() { Text = "Найти дубликаты и перенести оригиналы", AutoSize = true };
    private readonly Button pickSource = new() { Text = "Обзор...", AutoSize = true };
    private readonly Button pickDestination = new() { Text = "Обзор...", AutoSize = true };
    private static readonly int Variant = 4;

    public MainForm()
    {
        Text = "Задание " + Variant + " — перенос уникальных файлов";
        Width = 900; Height = 580; StartPosition = FormStartPosition.CenterScreen;
        var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 130, ColumnCount = 3, RowCount = 3, Padding = new Padding(8) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        header.Controls.Add(new Label { Text = "Источник", AutoSize = true }, 0, 0);
        header.Controls.Add(source, 1, 0); source.Dock = DockStyle.Fill;
        header.Controls.Add(pickSource, 2, 0);
        header.Controls.Add(new Label { Text = "Приёмник", AutoSize = true }, 0, 1);
        header.Controls.Add(destination, 1, 1); destination.Dock = DockStyle.Fill;
        header.Controls.Add(pickDestination, 2, 1);
        header.Controls.Add(run, 1, 2);
        Controls.Add(log); Controls.Add(header);
        pickSource.Click += (_, _) => Browse(source);
        pickDestination.Click += (_, _) => Browse(destination);
        run.Click += async (_, _) => await TransferAsync();
        FormClosing += (_, e) => { if (!run.Enabled) e.Cancel = true; }; // Дожидаемся окончания переноса.
    }

    private void Browse(TextBox target)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.SelectedPath;
    }

    private async Task TransferAsync()
    {
        string src, dst;
        try { src = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source.Text.Trim()));
              dst = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destination.Text.Trim())); }
        catch (Exception ex) { MessageBox.Show(ex.Message); return; }
        
        if (!Directory.Exists(src)) { MessageBox.Show("Папка-источник не найдена."); return; }
        if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase) ||
            dst.StartsWith(src.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            src.StartsWith(dst.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        { MessageBox.Show("Источник и приёмник не должны совпадать или содержать друг друга."); return; }
        run.Enabled = false; log.Text = "Выполняется обработка...";
        try
        {
            string report = await Task.Run(() => MoveUniqueFiles(src, dst));
            log.Text = report;
            if (Variant == 5)
            {
                using var save = new SaveFileDialog { Filter = "Текстовый отчёт|*.txt", FileName = "transfer_report.txt" };
                if (save.ShowDialog(this) == DialogResult.OK)
                {
                    await File.WriteAllTextAsync(save.FileName, report, Encoding.UTF8);
                    log.AppendText(Environment.NewLine + "Отчёт сохранён: " + save.FileName);
                }
            }
        }
        catch (Exception ex) { log.Text = "Ошибка: " + ex.Message; }
        finally { if (!IsDisposed) run.Enabled = true; }
    }

    private static string MoveUniqueFiles(string src, string dst)
    {
        var report = new StringBuilder();
        report.AppendLine("ОТЧЁТ О ПЕРЕНОСЕ ФАЙЛОВ");
        report.AppendLine("Источник: " + src);
        report.AppendLine("Приёмник: " + dst);
        report.AppendLine();
        var hashes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        int moved = 0, duplicates = 0, errors = 0;
        var files = Directory.EnumerateFiles(src, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = false, AttributesToSkip = FileAttributes.ReparsePoint }).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        Directory.CreateDirectory(dst);
        foreach (string file in files)
        {
            try
            {
                string hash;
                using (var stream = File.OpenRead(file))
                    hash = Convert.ToHexString(SHA256.HashData(stream));
                if (!hashes.TryGetValue(hash, out var known)) hashes[hash] = known = new List<string>();
                bool duplicate = known.Any(other => EqualFiles(file, other));
                if (duplicate)
                {
                    duplicates++;
                    report.AppendLine("ДУБЛИКАТ (оставлен): " + file);
                    continue;
                }
                
                string relative = Path.GetRelativePath(src, file);
                string target = Path.Combine(dst, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (File.Exists(target))
                {
                    string dir = Path.GetDirectoryName(target)!;
                    string stem = Path.GetFileNameWithoutExtension(target);
                    string ext = Path.GetExtension(target);
                    int suffix = 1;
                    do { target = Path.Combine(dir, $"{stem}_{suffix++}{ext}"); }
                    while (File.Exists(target));
                }
                File.Move(file, target);
                known.Add(target); // Оригинал теперь находится в приёмнике.
                moved++;
                report.AppendLine("ПЕРЕНЕСЁН: " + file + " -> " + target);
            }
            catch (Exception ex) { errors++; report.AppendLine("ОШИБКА: " + file + " — " + ex.Message); }
        }
        report.AppendLine();
        report.AppendLine($"Перенесено: {moved}; дубликатов оставлено: {duplicates}; ошибок: {errors}");
        return report.ToString();
    }

    private static bool EqualFiles(string a, string b)
    {
        using var x = File.OpenRead(a);
        using var y = File.OpenRead(b);
        if (x.Length != y.Length) return false;
        Span<byte> bx = stackalloc byte[8192];
        Span<byte> by = stackalloc byte[8192];
        int n;
        while ((n = x.Read(bx)) > 0)
        {
            int count = 0;
            while (count < n)
            {
                int k = y.Read(by[count..n]);
                if (k == 0) return false;
                count += k;
            }
            if (!bx[..n].SequenceEqual(by[..n])) return false;
        }
        return true;
    }
}
