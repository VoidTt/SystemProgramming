using System.Runtime.InteropServices;

internal static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hwnd, string text, string caption, uint type);

    private static void Main()
    {
        Console.Write("Имя: ");
        string name = Console.ReadLine() ?? "";
        Console.Write("Возраст: ");
        string age = Console.ReadLine() ?? "";
        Console.Write("Группа: ");
        string group = Console.ReadLine() ?? "";
        MessageBox(IntPtr.Zero, "Имя: " + name, "Обо мне", 0);
        MessageBox(IntPtr.Zero, "Возраст: " + age, "Обо мне", 0);
        MessageBox(IntPtr.Zero, "Группа: " + group, "Учёба", 0);
    }
}
