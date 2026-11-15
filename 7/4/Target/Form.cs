using System.Drawing;
using System.Windows.Forms;

namespace Homework;

public class MainForm : Form
{
    public MainForm()
    {
        Text = "Окно для изменения стилей";
        ClientSize = new Size(480, 300);
        StartPosition = FormStartPosition.CenterScreen;

        var label = new Label
        {
            Text = "Пример элементов управления",
            AutoSize = true,
            Location = new Point(45, 35),
            Font = new Font("Segoe UI", 11)
        };
        var input = new TextBox
        {
            Name = "textBox1",
            Text = "Введите текст",
            Location = new Point(45, 80),
            Width = 260
        };
        var button = new Button
        {
            Name = "button1",
            Text = "Нажми меня",
            Location = new Point(45, 135),
            Size = new Size(155, 42)
        };
        button.Click += (_, _) => MessageBox.Show("Кнопка работает!");
        Controls.AddRange(new Control[] { label, input, button });
    }
}
