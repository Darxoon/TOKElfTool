using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ElfLib;

namespace TOKElfTool.Editor
{
    /// <summary>
    /// Interaction logic for ObjectEditControl.xaml
    /// </summary>
    public partial class InnerEditor : Window
    {
        public List<Element<object>> DisplayObjects { get; set; }
        
        public GameDataType Type { get; set; }

        public Dictionary<ElfType, List<Element<object>>> Data { get; set; }
        public Dictionary<ElfType, List<long>> DataOffsets { get; set; }

        private EditorPanel editorPanel;
        
        public InnerEditor()
        {
            InitializeComponent();
        }

        private void InnerEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            Content = editorPanel = new EditorPanel
            {
                Type = Type,
                Objects = DisplayObjects,
                
                Data = Data,
                DataOffsets = DataOffsets,
                
                ChildHeaders = new string[]
                {
                    "Model Files Objects",
                    "State Objects",
                },
            };
        }

        private void InnerEditor_OnClosed(object sender, EventArgs e)
        {
            editorPanel.ApplyChangesToObject();
        }
    }
}
