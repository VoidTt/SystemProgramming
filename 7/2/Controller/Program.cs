using System.Runtime.InteropServices;

internal static class Program
{
    private const uint WM_SETTEXT = 0x000C;
    private const uint WM_CLOSE = 0x0010;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? className, string windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static void Main()
    {
        Console.Write("Заголовок окна для поиска [Мое окно]: ");
        string title = Console.ReadLine() is { Length: > 0 } input ? input : "Мое окно";
        IntPtr hwnd = FindWindow(null, title);
        if (hwnd == IntPtr.Zero)
        {
            Console.WriteLine("Окно не найдено. Сначала запустите Task2_Target.");
            return;
        }

        Console.WriteLine("1 — изменить заголовок; 2 — закрыть окно");
        Console.Write("Выбор: ");
        switch (Console.ReadLine())
        {
            case "1":
                Console.Write("Новый заголовок: ");
                SendMessage(hwnd, WM_SETTEXT, IntPtr.Zero, Console.ReadLine() ?? "");
                Console.WriteLine("Сообщение отправлено.");
                break;
            case "2":
                SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                Console.WriteLine("Команда закрытия отправлена.");
                break;
            default:
                Console.WriteLine("Неизвестная команда.");
                break;
        }
    }
}
