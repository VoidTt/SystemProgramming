namespace Homework;
public class MainForm : Form
{
    private readonly Button start = new() { Text = "Провести день", AutoSize = true };
    private readonly TextBox output = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private CancellationTokenSource? cancellation;
    public MainForm()
    {
        Text = "Казино — пять мест"; ClientSize = new Size(720,560);
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
        top.Controls.Add(start); Controls.Add(output); Controls.Add(top);
        start.Click += async (_, _) => {
            using var dialog = new SaveFileDialog { Filter = "Отчёт|*.txt", FileName = "casino_report.txt" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            start.Enabled = false; output.Text = "День начался...";
            cancellation?.Dispose(); cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            try {
                int total = Random.Shared.Next(20,101);
                string report = await Task.Run(() => Casino.Run(total, token));
                await File.WriteAllTextAsync(dialog.FileName, report, token);
                if (!IsDisposed) output.Text = report + "\r\nСохранено: " + dialog.FileName;
            }
            catch (Exception ex) { if (!IsDisposed) output.Text = ex.Message; }
            finally { if (!IsDisposed) start.Enabled = true; }
        };
        FormClosing += (_, _) => cancellation?.Cancel();
    }
}

