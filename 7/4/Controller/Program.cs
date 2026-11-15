using System.Runtime.InteropServices;
using System.Text;

internal static class Program
{
    private const int GWL_STYLE = -16;
    private const long ES_READONLY = 0x0800;
    private const long WS_DISABLED = 0x08000000;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint EM_SETREADONLY = 0x00CF;
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(IntPtr hwnd, bool enable);

    private static long GetStyle(IntPtr hwnd) => IntPtr.Size == 8
        ? GetWindowLongPtr64(hwnd, GWL_STYLE).ToInt64()
        : GetWindowLong32(hwnd, GWL_STYLE);

    private static void SetStyle(IntPtr hwnd, long style)
    {
        if (IntPtr.Size == 8)
            SetWindowLongPtr64(hwnd, GWL_STYLE, new IntPtr(style));
        else
            SetWindowLong32(hwnd, GWL_STYLE, unchecked((int)style));

        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    private static void Main()
    {
        IntPtr main = FindWindow(null, "Окно для изменения стилей");
        if (main == IntPtr.Zero)
        {
            Console.WriteLine("Окно не найдено. Запустите Task4_Target.");
            return;
        }

        IntPtr textBox = IntPtr.Zero;
        IntPtr button = IntPtr.Zero;
        // WinForms использует имена классов вида WindowsForms10.EDIT... и WindowsForms10.BUTTON...
        EnumChildWindows(main, (child, _) =>
        {
            var className = new StringBuilder(256);
            var caption = new StringBuilder(256);
            GetClassName(child, className, className.Capacity);
            GetWindowText(child, caption, caption.Capacity);
            string cls = className.ToString();
            string txt = caption.ToString();
            if (cls.Contains("EDIT", StringComparison.OrdinalIgnoreCase) && textBox == IntPtr.Zero)
                textBox = child;
            if (cls.Contains("BUTTON", StringComparison.OrdinalIgnoreCase) && button == IntPtr.Zero)
                button = child;
            return true;
        }, IntPtr.Zero);

        if (textBox != IntPtr.Zero)
        {
            SetStyle(textBox, GetStyle(textBox) | ES_READONLY);
            // EM_SETREADONLY уведомляет Edit-контрол о смене режима.
            SendMessage(textBox, EM_SETREADONLY, new IntPtr(1), IntPtr.Zero);
            Console.WriteLine("Текстовое поле переведено в режим чтения.");
        }
        else Console.WriteLine("Текстовое поле не найдено.");

        if (button != IntPtr.Zero)
        {
            SetStyle(button, GetStyle(button) | WS_DISABLED);
            EnableWindow(button, false);
            Console.WriteLine("Кнопка отключена.");
        }
        else Console.WriteLine("Кнопка не найдена.");
    }
}
