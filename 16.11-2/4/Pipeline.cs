using System.Text;
namespace Homework;

public static class Pipeline
{
    public static string Run(string folder, bool report, CancellationToken token)
    {
        Directory.CreateDirectory(folder);
        string all = Path.Combine(folder, "numbers.txt");
        string primes = Path.Combine(folder, "primes.txt");
        string sevens = Path.Combine(folder, "primes7.txt");
        string reportPath = Path.Combine(folder, "report.txt");
        using var fileMutex = new Mutex();
        Exception? error = null;

        // Join задаёт порядок этапов; Mutex защищает доступ к файлам.
        Thread Create(Thread? previous, Action action) => new(() =>
        {
            previous?.Join();
            fileMutex.WaitOne();
            try
            {
                if (error != null) return;
                token.ThrowIfCancellationRequested();
                action();
            }
            catch (Exception ex) { error = ex; }
            finally { fileMutex.ReleaseMutex(); }
        }) { IsBackground = true };

        var first = Create(null, () => {
            var numbers = new List<int>();
            for (int i = 0; i < 200; i++) { token.ThrowIfCancellationRequested(); numbers.Add(Random.Shared.Next(1, 10001)); }
            File.WriteAllLines(all, numbers.Select(n => n.ToString()));
        });
        var second = Create(first, () => {
            var numbers = File.ReadAllLines(all).Select(int.Parse).Where(IsPrime);
            File.WriteAllLines(primes, numbers.Select(n => n.ToString()));
        });
        var third = Create(second, () => {
            var numbers = File.ReadAllLines(primes).Select(int.Parse).Where(n => n % 10 == 7);
            File.WriteAllLines(sevens, numbers.Select(n => n.ToString()));
        });
        var fourth = Create(third, () => {
            var text = new StringBuilder();
            foreach (string file in new[] { all, primes, sevens })
            {
                text.AppendLine("Файл: " + Path.GetFileName(file));
                text.AppendLine("Количество чисел: " + File.ReadAllLines(file).Length);
                text.AppendLine("Размер в байтах: " + new FileInfo(file).Length);
                text.AppendLine("Содержимое:");
                text.AppendLine(File.ReadAllText(file));
            }
            File.WriteAllText(reportPath, text.ToString(), Encoding.UTF8);
        });
        first.Start(); second.Start(); third.Start();
        if (report) fourth.Start();
        if (report) fourth.Join(); else third.Join();
        if (error != null) throw error;
        return report ? File.ReadAllText(reportPath) : $"Созданы файлы:\r\n{all}\r\n{primes}\r\n{sevens}";
    }

    public static bool IsPrime(int number)
    {
        if (number < 2) return false;
        for (int d = 2; d <= number / d; d++) if (number % d == 0) return false;
        return true;
    }
}

