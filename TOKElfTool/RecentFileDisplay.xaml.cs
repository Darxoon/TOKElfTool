using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for RecentFileDisplay.xaml
    /// </summary>
    public partial class RecentFileDisplay : UserControl
    {
        public event EventHandler OnEntryClick;

        public RecentFileDisplay()
        {
            InitializeComponent();
        }

        public void UpdateList(int maxAmount, string[] entries)
        {
            ClearEntries();

            if (entries.Length == 0)
            {
                label.Content = "No recent files";
                return;
            }

            label.Content = "Recently opened files:";

            IEnumerable<string> cappedEntries = entries.Take(maxAmount);

            GenerateEntries(cappedEntries);
        }

        private void ClearEntries()
        {
            for (int i = stackPanel.Children.Count - 1; i >= 1; i--)
            {
                stackPanel.Children.RemoveAt(i);
            }
        }

        private void GenerateEntries(IEnumerable<string> entries)
        {
            foreach (string entryName in entries)
            {
                Label label = new Label
                {
                    Content = Util.ShortenPath(entryName), 
                    Foreground = Brushes.Blue, 
                    Style = FindResource("Underlined") as Style,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                stackPanel.Children.Add(label);
            }
        }
    }
}
