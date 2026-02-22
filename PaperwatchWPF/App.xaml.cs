using System.Windows;

namespace PaperwatchWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            MainWindow mainWindow = new MainWindow();
            
            if (e.Args.Length > 0)
            {
                mainWindow.InitialFile = e.Args[0];
            }
            
            mainWindow.Show();
        }
    }
}
