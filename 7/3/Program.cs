using System.Runtime.InteropServices;
using System.Threading;

internal static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Beep(uint frequency, uint duration);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MessageBeep(uint type);

    private static void Main()
    {
        Console.WriteLine("Последовательность звуковых сигналов:");
        for (int i = 0; i < 3; i++)
        {
            foreach (uint frequency in new uint[] { 500, 700, 900 })
            {
                if (!Beep(frequency, 300))
                    Console.WriteLine($"Beep({frequency}) не удалось воспроизвести.");
                Thread.Sleep(500);
            }
        }
        MessageBeep(0x40);
        Thread.Sleep(1000);
        MessageBeep(0x30);
        Console.WriteLine("Готово. Системные звуки зависят от настроек Windows.");
    }
}
