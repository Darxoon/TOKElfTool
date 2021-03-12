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
using System.Windows.Shapes;
using ElfLib;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for ObjectTypeSelector.xaml
    /// </summary>
    public partial class ObjectTypeSelector : Window
    {
        private bool submitted;

        public ObjectTypeSelector()
        {
            InitializeComponent();
        }

        public new static GameDataType? Show()
        {
            ObjectTypeSelector typeSelector = new ObjectTypeSelector();
            typeSelector.ShowDialog();

            if (typeSelector.submitted == true)
                return (GameDataType)typeSelector.SelectionBox.SelectedIndex;
            else
                return null;
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            submitted = true;
            Close();
        }

        private void CancelButton_Onclick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
