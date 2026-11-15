using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

internal static class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child")
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Ожидаются путь к файлу и слово");
                return 1;
            }
            string path = args[1];
            string word = args[2];
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Файл не найден: {path}");
                return 2;
            }
            if (string.IsNullOrWhiteSpace(word))
            {
                Console.Error.WriteLine("Слово не может быть пустым");
                return 3;
            }
            try
            {
                string content = File.ReadAllText(path);
                string pattern = @"(?<![\p{L}\p{N}_])" + Regex.Escape(word) + @"(?![\p{L}\p{N}_])";
                int count = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
                Console.WriteLine($"Файл: {path}");
                Console.WriteLine($"Искомое слово: {word}");
                Console.WriteLine($"Количество вхождений: {count}");
                return 0;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Ошибка чтения файла: {ex.Message}");
                return 4;
            }
        }

        string sample = Path.Combine(AppContext.BaseDirectory, "sample.txt");
        Console.Write($"Путь к файлу ({sample}): ");
        string pathInput = Console.ReadLine() ?? "";
        string pathToSearch = string.IsNullOrWhiteSpace(pathInput) ? sample : Path.GetFullPath(pathInput.Trim('"'));
        Console.Write("Слово (Enter = bicycle): ");
        string wordInput = Console.ReadLine() ?? "";
        string wordToSearch = string.IsNullOrWhiteSpace(wordInput) ? "bicycle" : wordInput.Trim();
        string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Не найден путь к программе");
        var info = new ProcessStartInfo(executable) { UseShellExecute = false };
        info.ArgumentList.Add("--child");
        info.ArgumentList.Add(pathToSearch);
        info.ArgumentList.Add(wordToSearch);
        using var child = Process.Start(info) ?? throw new InvalidOperationException("Не удалось запустить процесс");
        child.WaitForExit();
        Console.WriteLine($"Код завершения дочернего процесса: {child.ExitCode}");
        return 0;
    }
}
