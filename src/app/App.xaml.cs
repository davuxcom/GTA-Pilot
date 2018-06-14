using System.Windows;

namespace GTAPilot
{
    public partial class App : Application
    {
        public static int FPS = 1000 / 10;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
