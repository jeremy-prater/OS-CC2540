using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HM_10_SDI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new OSCC2540());
        }
    }
}
