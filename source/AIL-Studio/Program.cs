using System;
using System.Windows.Forms;

namespace AIL_Studio
{
    /// <summary>Entry point for the AIL Studio WinForms IDE.</summary>
    static class Program
    {
        /// <summary>
        /// Application entry point. Enables visual styles and launches the <see cref="MainForm"/>.
        /// Must run on a single-threaded apartment (STA) thread as required by WinForms.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
