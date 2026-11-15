using System;
using System.Diagnostics;
using System.Threading;

internal static class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child")
        {
            Console.WriteLine("работает...");
            Thread.Sleep(15000);
            Console.WriteLine("завершился сам");
            return 12;
        }

        Console.WriteLine("1 — ждать завершения; 2 — принудительно завершить");
        Console.Write("Выбор: ");
        string? choice = Console.ReadLine();
        if (choice != "1" && choice != "2")
        {
            Console.WriteLine("Неверный выбор");
            return 1;
        }

        string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Не найден путь к программе");
        using var child = Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            ArgumentList = { "--child" }
        }) ?? throw new InvalidOperationException("Не удалось запустить процесс");
        Console.WriteLine($"PID={child.Id}.");

        if (choice == "1")
        {
            child.WaitForExit();
            Console.WriteLine($"Код завершения: {child.ExitCode}");
        }
        else
        {
            Thread.Sleep(1000);
            if (!child.HasExited)
            {
                child.Kill();
                child.WaitForExit();
                Console.WriteLine("Принудительно завершён");
            }
            else
                Console.WriteLine("Завершился");
            Console.WriteLine($"Код завершения: {child.ExitCode}");
        }
        return 0;
    }
}
