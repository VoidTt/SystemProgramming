namespace Homework;
public class MainForm : Form
{
    private readonly bool withReport = true;
    private readonly TextBox folder = new() { Width = 590, Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NumberFiles") };
    private readonly Button start = new() { Text = "Старт", AutoSize = true };
    private readonly TextBox output = new() { Width = 630, Height = 390, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both };
    private CancellationTokenSource? cancellation;
    public MainForm()
    {
        Text = withReport ? "Потоки, мьютекс и отчёт" : "Потоки и мьютекс";
        ClientSize = new Size(680,540);
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12) };
        Controls.Add(panel);
        panel.Controls.Add(new Label { Text = "Папка результатов (каждый запуск создаёт новую подпапку)", AutoSize = true });
        panel.Controls.Add(folder); panel.Controls.Add(start); panel.Controls.Add(output);
        start.Click += async (_, _) => {
            if (string.IsNullOrWhiteSpace(folder.Text)) { MessageBox.Show("Укажите папку."); return; }
            start.Enabled = false;
            cancellation?.Dispose(); cancellation = new CancellationTokenSource();
            try {
                string target = Path.Combine(Path.GetFullPath(folder.Text), DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..6]);
                string text = await Task.Run(() => Pipeline.Run(target, withReport, cancellation.Token));
                if (!IsDisposed) output.Text = "Папка: " + target + "\r\n" + text;
            }
            catch (Exception ex) { if (!IsDisposed) output.Text = ex.Message; }
            finally { if (!IsDisposed) start.Enabled = true; }
        };
        FormClosing += (_, _) => cancellation?.Cancel();
    }
}

