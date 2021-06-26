using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for CrashReportWidnow.xaml
    /// </summary>
    public partial class CrashReportWindow : Window
    {
        #region P/Invoke for hiding close button
        // ReSharper disable InconsistentNaming IdentifierTypo
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        // ReSharper restore InconsistentNaming IdentifierTypo
        #endregion
        
        private string errorString = null;
        
        public CrashReportWindow()
        {
            InitializeComponent();

            icon.Source = Imaging.CreateBitmapSourceFromHIcon(
                SystemIcons.Error.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        public CrashReportWindow(Exception e) : this()
        {
            errorString = e.ToString() + "\n" + e.StackTrace;
        }
        
        private void CrashReportWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            #region P/Invoke for hiding close button
            // ReSharper disable InconsistentNaming IdentifierTypo
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            // ReSharper restore InconsistentNaming IdentifierTypo
            #endregion
            
            textBox.Text = errorString;
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void CopyToClipboard_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(textBox.Text);
        }

        private void Quit_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Restart_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void Continue_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
