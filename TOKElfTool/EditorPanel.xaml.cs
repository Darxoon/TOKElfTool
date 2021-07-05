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
using ElfLib;
using ElfLib.CustomDataTypes;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for EditorPanel.xaml
    /// </summary>
    public partial class EditorPanel : UserControl
    {
        private static string GetTypeName(GameDataType type) => type switch
        {
            GameDataType.Maplink => "Maplink Node",
            GameDataType.DataNpc => "NPC Type",
            GameDataType.DataItem => "Item Type",
            _ => type.ToString(),
        };
        
        public GameDataType Type { get; set; }
        
        public List<Element<object>> Objects { get; set; }
        
        public List<Symbol> SymbolTable { get; set; }

        public event EventHandler OnUnsavedChanges;
        
        public bool HasUnsavedChanges { get; set; }
        
        public EditorPanel()
        {
            InitializeComponent();
        }
        
        private List<bool> modifiedObjects = new List<bool>();

        private void EditorPanel_OnInitialized(object sender, EventArgs e)
        {
            modifiedObjects = new List<bool>(new bool[Objects.Count]);
            InitializeObjectsPanel(GetTypeName(Type));
        }

        private void InitializeObjectsPanel(string objectName)
        {
            
            for (int i = 0; i < Objects.Count; i++)
            {
                object currentObject = Objects[i].value;

                ObjectEditControl control = new ObjectEditControl(currentObject, $"{objectName} {i}", i, SymbolTable);

                control.RemoveButtonClick += RemoveButton_OnClick;
                control.DuplicateButtonClick += DuplicateButton_OnClick;
                control.ViewButtonClick += ViewButton_OnClick;

                control.ValueChanged += (sender, args) =>
                {
                    if (!HasUnsavedChanges)
                        OnUnsavedChanges?.Invoke(this, EventArgs.Empty);
                    HasUnsavedChanges = true;
                    
                    searchBar.HasIndexed = false;
                    modifiedObjects[control.Index] = true;
                };

                objectTabPanel.Children.Add(control);
            }

        }

        private void FixExpanderIndexes()
        {
            for (int i = 0; i < objectTabPanel.Children.Count; i++)
            {
                ObjectEditControl expander = (ObjectEditControl)objectTabPanel.Children[i];
                expander.Index = i;
                expander.Header = Type == GameDataType.Maplink && i == objectTabPanel.Children.Count - 1 ? "Maplink Header (Advanced)" : $"{GetTypeName(Type)} {i}";
            }
        }
        
        // Add / Remove
        private void ApplyInstancePanelChanges(int amountChange)
        {
            if (!HasUnsavedChanges)
                OnUnsavedChanges?.Invoke(this, EventArgs.Empty);
            HasUnsavedChanges = true;
            searchBar.HasIndexed = false;

            FixExpanderIndexes();

            if (Type == GameDataType.Maplink)
                UpdateMaplinkHeaderChildCount(amountChange);
        }
        
        private void UpdateMaplinkHeaderChildCount(int nodeAmountChange)
        {
            UIElementCollection children = objectTabPanel.Children;

            if (children.Count == 0)
                return;

            ObjectEditControl headerControl = (ObjectEditControl)children.Last();
            headerControl.Generate();
            
            TextBox lastElementIndexEdit = (TextBox)headerControl.Grid.Children[3];
            lastElementIndexEdit.Text = (int.Parse(lastElementIndexEdit.Text) + nodeAmountChange).ToString();
        }
        
        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            ObjectEditControl objectEditControl = (ObjectEditControl)sender;

            bool? result = MyMessageBox.Show(Window.GetWindow(this), $"Are you sure you want to delete this {GetTypeName(Type)}?", "TOK ELF Editor", MessageBoxResult.Yes);
            if (result == true)
            {
                objectTabPanel.Children.Remove(objectEditControl);
                ApplyInstancePanelChanges(-1);
            }
        }
        
        private void DuplicateButton_OnClick(object sender, RoutedEventArgs e)
        {
            ObjectEditControl sourceControl = (ObjectEditControl)sender;
            
            int objectIndex = objectTabPanel.Children.IndexOf(sourceControl);
            
            ObjectEditControl clone = sourceControl.Clone();
            clone.IsExpanded = false;
            objectTabPanel.Children.Insert(objectIndex, clone);

            // update modified objects (yes, results in slower save times)
            modifiedObjects.Add(true);
            for (int i = objectIndex; i < modifiedObjects.Count; i++)
            {
                modifiedObjects[i] = true;
            }
            
            ApplyInstancePanelChanges(1);
        }
        
        private void Button_RemoveAllObjects_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        
        private void Button_AddObject_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        
        // Search
        private ObjectEditControl[] searchResultControls = null;
        
        private void SearchBar_OnStartIndexing(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
        
        private void SearchBar_OnOnSearch(object sender, string e)
        {
            throw new NotImplementedException();
        }

        private void UndoSearch()
        {
            for (int i = 0; i < searchResultControls.Length; i++)
            {
                ObjectEditControl control = searchResultControls[i];
                control.ViewButtonVisible = false;
                control.ModifyButtonsEnabled = true;
                searchResultPanel.Children.Remove(control);
                objectTabPanel.Children.Insert(control.Index, control);
            }

            searchResultControls = null;
            objectTabPanel.Visibility = Visibility.Visible;
            searchResultPanel.Visibility = Visibility.Collapsed;
        }
        
        private void ViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            ObjectEditControl control = (ObjectEditControl)sender;

            searchBar.Text = "";
            
            if (searchResultControls != null)
                UndoSearch();
            
            control.BringIntoView();
        }
    }
}
