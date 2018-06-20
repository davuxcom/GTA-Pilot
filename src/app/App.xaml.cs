using System.Windows;

namespace GTAPilot
{
    public partial class App : Application
    {
        public static int FPS = 1000 / 30;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
           // new MainWindow().Show();
            new FlightPlanBuidler().Show();
        }
    }
}
