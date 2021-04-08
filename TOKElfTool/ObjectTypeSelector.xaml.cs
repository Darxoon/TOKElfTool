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

        public static GameDataType? Show(GameDataType defaultValue = GameDataType.None)
        {
            ObjectTypeSelector typeSelector = new ObjectTypeSelector
            {
                SelectionBox = {SelectedIndex = (int)defaultValue}
            };
            typeSelector.ShowDialog();

            if (typeSelector.submitted == true)
                return (GameDataType)typeSelector.SelectionBox.SelectedIndex;
            else
                return null;
        }

        public static GameDataType? Show(string fileName)
        {
            switch (fileName)
            {
                case "dispos_Npc":
                    return Show(GameDataType.NPC);
                case "dispos_Mobj":
                    return Show(GameDataType.Mobj);
                case "dispos_Aobj":
                    return Show(GameDataType.Aobj);
                default:
                    return Show();
            }
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
