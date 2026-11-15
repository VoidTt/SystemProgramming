using System;
using System.Windows.Forms;
using Homework;

namespace Homework;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
