using System;
using System.IO;
using System.Windows.Forms;

namespace LunarAccountManager
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string path;
            if (args.Length > 0 && args[0].Trim().Length > 0)
                path = args[0];
            else
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                    @".lunarclient\settings\game\accounts.json");

            var store = new AccountsStore(path);
            Application.Run(new MainForm(store));
        }
    }
}
