using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ElfLib;
using ElfLib.Binary;
using ElfLib.Types.Disposition;
using ElfLib.Types.Registry;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using TOKElfTool.Search;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for EditorPanel.xaml
    /// </summary>
    public partial class EditorPanel : UserControl
    {
        private static string GetTypeName(Element<object> instance) => instance.value switch
        {
            MaplinkNode _ => "Maplink Node",
            NpcType _ => "NPC Type",
            ItemType _ => "Item Type",
            NpcModel _ => "NPC Model",
            NpcModelFiles _ => "Files Object",
            NpcModelState _ => "State Object",
            
            _ => instance.value.GetType().Name,
        };
        
        public event EventHandler<(ElfType type, int index)> HyperlinkClick;
        
        public GameDataType Type { get; set; }
        
        public Type DefaultType { get; set; }
        
        public List<Element<object>> Objects { get; set; }
        
        public Dictionary<ElfType, List<long>> DataOffsets { get; set; }
        
        public List<Symbol> SymbolTable { get; set; }

        public event EventHandler OnUnsavedChanges;
        
        public bool HasUnsavedChanges { get; set; }
        
        public EditorPanel()
        {
            InitializeComponent();
        }
        
        private List<bool> modifiedObjects = new List<bool>();

        private void EditorPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            modifiedObjects = new List<bool>(new bool[Objects.Count]);
            InitializeObjectsPanel();
        }

        public void FocusObject(int index)
        {
            ObjectEditControl control = (ObjectEditControl)objectTabPanel.Children[index];
            control.BringIntoView();
            control.IsExpanded = true;
        }

        private void InitializeObjectsPanel()
        {
            
            for (int i = 0; i < Objects.Count; i++)
            {
                Element<object> currentElement = Objects[i];

                string title = Objects[i].value is MaplinkHeader ? "Maplink Header (Advanced)" : $"{GetTypeName(currentElement)} {i}";
                ObjectEditControl control = new ObjectEditControl(currentElement.value, title, i, SymbolTable, DataOffsets);

                control.RemoveButtonClick += RemoveButton_OnClick;
                control.DuplicateButtonClick += DuplicateButton_OnClick;
                control.ViewButtonClick += ViewButton_OnClick;

                control.HyperlinkClick += (sender, e) =>
                {
                    HyperlinkClick?.Invoke(sender, e);
                };

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

        public void CollectObjects(ElfBinary<object> binary)
        {
            ObjectCollector<object> collector = new ObjectCollector<object>(binary, modifiedObjects);
            collector.CollectObjects(objectTabPanel.Children);
        }

        private void FixExpanderIndexes()
        {
            for (int i = 0; i < objectTabPanel.Children.Count; i++)
            {
                ObjectEditControl expander = (ObjectEditControl)objectTabPanel.Children[i];
                expander.Index = i;
                expander.Header = Objects[i].value is MaplinkHeader ? "Maplink Header (Advanced)" : $"{GetTypeName(Objects[i])} {i}";
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
            ObjectEditControl control = (ObjectEditControl)sender;

            bool? result = MyMessageBox.Show(Window.GetWindow(this), $"Are you sure you want to delete this {GetTypeName(Objects[control.Index])}?", "TOK ELF Editor", MessageBoxResult.Yes);
            if (result == true)
            {
                objectTabPanel.Children.RemoveAt(control.Index);
                Objects.RemoveAt(control.Index);
                modifiedObjects.RemoveAt(control.Index);
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
            Objects.Insert(objectIndex, new Element<object>(Activator.CreateInstance(DefaultType)));
            modifiedObjects.Insert(objectIndex, true);
            
            ApplyInstancePanelChanges(1);
        }
        
        private void Button_RemoveAllObjects_OnClick(object sender, RoutedEventArgs e)
        {
            int originalAmount = objectTabPanel.Children.Count;
            
            // Reverse loop that removes all objects
            for (int i = objectTabPanel.Children.Count - 1; i >= 0; i--)
            {
                objectTabPanel.Children.RemoveAt(i);
            }

            modifiedObjects.Clear();
            ApplyInstancePanelChanges(originalAmount - 1);
        }
        
        private void Button_AddObject_OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            UIElementCollection children = objectTabPanel.Children;

            object instance = Activator.CreateInstance(DefaultType);
            ObjectEditControl clone = new ObjectEditControl(instance, "", objectTabPanel.Children.Count, SymbolTable, DataOffsets)
            {
                IsExpanded = true, 
                Index = children.Count,
            };
            
            clone.HyperlinkClick += (_, e2) =>
            {
                HyperlinkClick?.Invoke(this, e2);
            };


            if (Type != GameDataType.Maplink)
                children.Add(clone);
            else
            {
                children.Insert(children.Count - 1, clone);
                modifiedObjects[modifiedObjects.Count - 1] = true;
            }

            clone.BringIntoView();

            Objects.Add(new Element<object>(instance));
            modifiedObjects.Add(true);
            ApplyInstancePanelChanges(1);
        }
        
        // Search
        private ObjectEditControl[] searchResultControls;
        
        private SearchIndex searchIndex;
        
        private void SearchBar_OnStartIndexing(object sender, EventArgs e)
        {
            searchIndex = new SearchIndex(Objects.Select(element => element.value).ToArray());
        }
        
        private void SearchBar_OnOnSearch(object sender, string e)
        {
            if (searchResultControls != null)
                UndoSearch();

            if (searchBar.Text != "")
                Search(searchBar.Text.ToLower());
        }
        
        private void Search(string text)
        {
            PhraseQuery phraseQuery = new PhraseQuery {
                new Term("name", text),
            };

            // TODO: Move into SearchIndex
            IndexReader reader = searchIndex.Reader;
            IndexSearcher searcher = new IndexSearcher(reader);
            ScoreDoc[] hits = searcher.Search(phraseQuery, 20).ScoreDocs;

            Document[] documents = hits.Select(hit => searcher.Doc(hit.Doc)).ToArray();

            int[] indices = documents.Select(document => int.Parse(document.Get("index"))).Distinct().ToArray();

            int[] orderedIndices = (int[])indices.Clone();
            Array.Sort(orderedIndices);

            ObjectEditControl[] controls = new ObjectEditControl[indices.Length];
            searchResultControls = new ObjectEditControl[indices.Length];

            for (int i = orderedIndices.Length - 1; i >= 0; i--)
            {
                ObjectEditControl control = (ObjectEditControl)objectTabPanel.Children[orderedIndices[i]];
                control.ViewButtonVisible = true;
                control.ModifyButtonsEnabled = false;
                objectTabPanel.Children.RemoveAt(orderedIndices[i]);
                controls[Array.IndexOf(indices, orderedIndices[i])] = control;
                searchResultControls[i] = control;
            }

            objectTabPanel.Visibility = Visibility.Collapsed;
            searchResultPanel.Visibility = Visibility.Visible;
            searchResultPanel.Children.Clear();

            for (int i = 0; i < controls.Length; i++)
            {
                searchResultPanel.Children.Add(controls[i]);
            }
        }

        public void UndoSearch()
        {
            if (searchResultControls == null)
                return;
            
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

        public void CollapseAllObjects()
        {
            foreach (ObjectEditControl control in objectTabPanel.Children)
            {
                control.IsExpanded = false;
            }
        }
        
        public void ExpandAllObjects()
        {
            foreach (ObjectEditControl control in objectTabPanel.Children)
            {
                control.IsExpanded = true;
            }
        }
    }
}
