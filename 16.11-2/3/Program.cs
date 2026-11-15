namespace Homework;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Именованный семафор общий для всех копий программы.
        using var slots = new Semaphore(3, 3, @"Local\TTT_16_11_ThreeCopies");
        if (!slots.WaitOne(0))
        {
            MessageBox.Show("Уже запущены три копии. Четвёртая копия будет закрыта.");
            return;
        }
        try
        {
            Application.Run(new Form {
                Text = "Копия приложения: " + Environment.ProcessId,
                ClientSize = new Size(430,180),
                StartPosition = FormStartPosition.CenterScreen,
                Controls = { new Label { Text = "Одновременно можно открыть не более трёх копий.\nЗакройте это окно, чтобы освободить место.", AutoSize = true, Location = new Point(20,40) } }
            });
        }
        finally { slots.Release(); }
    }
}

