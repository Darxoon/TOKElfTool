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

        public static GameDataType? Show(Window parent, GameDataType defaultValue = GameDataType.None)
        {
            ObjectTypeSelector typeSelector = new ObjectTypeSelector
            {
                SelectionBox = {SelectedIndex = (int)defaultValue},
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = parent,
            };
            typeSelector.ShowDialog();

            if (typeSelector.submitted == true)
                return (GameDataType)typeSelector.SelectionBox.SelectedIndex;
            else
                return null;
        }

        public static GameDataType? Show(Window parent, string fileName)
        {
            switch (fileName)
            {
                case "dispos_Npc":
                    return Show(parent, GameDataType.NPC);
                case "dispos_Mobj":
                    return Show(parent, GameDataType.Mobj);
                case "dispos_Aobj":
                    return Show(parent, GameDataType.Aobj);
                case "dispos_BShape":
                    return Show(parent, GameDataType.BShape);
                case "dispos_Item":
                    return Show(parent, GameDataType.Item);
                case "maplink":
                    return Show(parent, GameDataType.Maplink);
                default:
                    return Show(parent);
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
