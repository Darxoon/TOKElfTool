using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for MyMessageBox.xaml
    /// </summary>
    public partial class MyMessageBox : Window
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

        public MyMessageBox(Icon messageIcon, bool showYes = true, bool showNo = true, bool showOk = false)
        {
            InitializeComponent();

            icon.Source = Imaging.CreateBitmapSourceFromHIcon(
                messageIcon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (showYes == false)
                YesButton.Visibility = Visibility.Collapsed;
            if (showNo == false)
                NoButton.Visibility = Visibility.Collapsed;
            if (showOk == false)
                OkButton.Visibility = Visibility.Collapsed;

            Loaded += (sender, args) =>
            {
                #region P/Invoke for hiding close button
                // ReSharper disable InconsistentNaming IdentifierTypo
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
                // ReSharper restore InconsistentNaming IdentifierTypo
                #endregion
            };
        }

        public MyMessageBox() : this(SystemIcons.Exclamation) { }

        private bool? result = false;

        public static bool? Show(Window owner, string mainText, string title,
            MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult) =>
            Show(owner, mainText, title, defaultResult, buttons, icon);

        public static bool? Show(Window owner, string mainText, string title, 
            MessageBoxResult defaultResult, MessageBoxButton buttons = MessageBoxButton.YesNo, 
            MessageBoxImage icon = MessageBoxImage.Warning)
        {
            Icon messageIcon;
            switch (icon)
            {
                case MessageBoxImage.Warning:
                    messageIcon = SystemIcons.Warning;
                    break;
                case MessageBoxImage.Error:
                    messageIcon = SystemIcons.Error;
                    break;
                case MessageBoxImage.Asterisk:
                    messageIcon = SystemIcons.Asterisk;
                    break;
                case MessageBoxImage.None:
                    messageIcon = null;
                    break;
                case MessageBoxImage.Question:
                    messageIcon = SystemIcons.Question;
                    break;
                default:
                    messageIcon = SystemIcons.Exclamation;
                    break;
            }

            MyMessageBox myMessageBox = new MyMessageBox(messageIcon, 
                buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel,
                buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel,
                buttons == MessageBoxButton.OK || buttons == MessageBoxButton.OKCancel)
            {
                Owner = owner, 
                Title = title, 
                MainText =
                {
                    Text = mainText,
                },
            };
            switch (defaultResult)
            {
                case MessageBoxResult.Yes:
                    myMessageBox.YesButton.Focus();
                    break;
                case MessageBoxResult.No:
                    myMessageBox.NoButton.Focus();
                    break;
                // Unnecessary because MyMessageBox doesn't have those buttons
                case MessageBoxResult.None:
                    break;
                case MessageBoxResult.OK:
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
            myMessageBox.ShowDialog();

            owner.Focus();

            return myMessageBox.result;
        }

        public static async Task<bool?> ShowAsync(Window owner, string mainText, string title, string selectedButton)
        {
            MyMessageBox myMessageBox = new MyMessageBox
            {
                Owner = owner,
            };
            await Task.Run(() => myMessageBox.ShowDialog());

            return myMessageBox.result;
        }

        [SuppressMessage("ReSharper", "PossibleUnintendedReferenceComparison")]
        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == NoButton)
                result = false;
            else if (sender == YesButton)
                result = true;
            else if (sender == OkButton)
                result = null;
            else
                result = null;

            Close();
        }
    }
}
