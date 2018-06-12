using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GTAPilot
{
    public partial class App : Application
    {
        internal SystemManager InputManager;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InputManager = new SystemManager();

            var mainWindow = new MainWindow(new MainWindowViewModel(InputManager));
            mainWindow.Show();
        }
    }
}
