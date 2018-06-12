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
          //  var srcSelect = new SourceSelectionDialog();
           // srcSelect.ShowDialog();

          //  if (srcSelect.Result == null)
            {
          //      InputManager = new SystemManager(new ReplayFrameProducer(@"c:\save\recording1"));
            }
         //   else
            {

            }


            //  var mainWindow = new MainWindow(new MainWindowViewModel(InputManager));
            //  mainWindow.Show();

            var t = new TestWindow();
            t.Show();
        }
    }
}
