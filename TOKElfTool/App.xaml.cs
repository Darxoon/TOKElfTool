using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            Current.DispatcherUnhandledException += App_OnUnhandledException;
#endif

            base.OnStartup(e);
        }

        private void App_OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            CrashReportWindow window = new CrashReportWindow(e.Exception)
            {
                Owner = MainWindow,
            };
            window.ShowDialog();

            e.Handled = true;
        }
    }
}
