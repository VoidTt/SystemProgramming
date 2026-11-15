using System;
using System.Diagnostics;
using System.Globalization;

internal static class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child")
        {
            if (args.Length != 4 ||
                !double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
            {
                Console.Error.WriteLine("Ожидаются: число число операция");
                return 1;
            }
            string operation = args[3];
            double result;
            switch (operation)
            {
                case "+": result = a + b; break;
                case "-": result = a - b; break;
                case "*": result = a * b; break;
                case "/" when b != 0: result = a / b; break;
                case "/": Console.Error.WriteLine("Деление на ноль!"); return 2;
                default: Console.Error.WriteLine("Неизвестная операция"); return 3;
            }
            Console.WriteLine($"Аргументы: {a.ToString(CultureInfo.InvariantCulture)}, {b.ToString(CultureInfo.InvariantCulture)}, {operation}");
            Console.WriteLine($"Результат: {result.ToString(CultureInfo.InvariantCulture)}");
            return 0;
        }

        Console.Write("Первое число: ");
        string aText = Console.ReadLine() ?? "";
        Console.Write("Второе число: ");
        string bText = Console.ReadLine() ?? "";
        Console.Write("Операция (+, -, *, /): ");
        string op = Console.ReadLine() ?? "";
        if (!double.TryParse(aText.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double aNumber) ||
            !double.TryParse(bText.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double bNumber))
        {
            Console.WriteLine("Ошибка: введите два числа");
            return 1;
        }
        string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Не найден путь к программе");
        var info = new ProcessStartInfo(executable) { UseShellExecute = false };
        info.ArgumentList.Add("--child");
        info.ArgumentList.Add(aNumber.ToString(CultureInfo.InvariantCulture));
        info.ArgumentList.Add(bNumber.ToString(CultureInfo.InvariantCulture));
        info.ArgumentList.Add(op);
        using var child = Process.Start(info) ?? throw new InvalidOperationException("Не удалось запустить процесс");
        child.WaitForExit();
        Console.WriteLine($"Код завершения процесса: {child.ExitCode}");
        return 0;
    }
}
