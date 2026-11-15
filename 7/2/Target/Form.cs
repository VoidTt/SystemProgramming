using System.Drawing;
using System.Windows.Forms;

namespace Homework;

public class MainForm : Form
{
    public MainForm()
    {
        Text = "Мое окно";
        ClientSize = new Size(480, 240);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(new Label
        {
            Text = "Это окно управляется из Task2_WindowControl.\nМожно изменить заголовок или закрыть окно.",
            AutoSize = true,
            Location = new Point(35, 55),
            Font = new Font("Segoe UI", 11)
        });
    }
}
