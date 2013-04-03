using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace VM_Manager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.Run(new TrayContext(args));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Es ist ein Fehler aufgetreten", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
