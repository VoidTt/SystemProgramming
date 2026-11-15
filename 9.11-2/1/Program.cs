using System;
using System.Diagnostics;
using System.Threading;

internal static class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child")
        {
            Console.WriteLine("PID: " + Environment.ProcessId);
            Thread.Sleep(2000);
            Console.WriteLine("Завершает работу с кодом 7.");
            return 7;
        }

        string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Не найден путь к программе");
        using var child = Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            ArgumentList = { "--child" }
        }) ?? throw new InvalidOperationException("Не удалось запустить процесс");
        Console.WriteLine($"Родитель запустил дочерний процесс PID={child.Id}.");
        child.WaitForExit();
        Console.WriteLine($"Дочерний процесс завершился: {child.ExitCode}");
        return 0;
    }
}
