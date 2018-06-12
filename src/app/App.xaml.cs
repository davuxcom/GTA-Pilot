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
        internal InputManager InputManager;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InputManager = new InputManager();

            var mainWindow = new MainWindow(new MainWindowViewModel(InputManager));
            mainWindow.Show();
        }
    }
}
