using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ElfLib;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElfLib.CustomDataTypes.NPC;
using ZstdNet;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EmptyLabel.Visibility = Visibility.Visible;
            ScrollViewer.Visibility = Visibility.Collapsed;

            // create AppData path
            if (!Directory.Exists(Path.Combine(DataFolderPath, "TOKElfTool")))
            {
                Directory.CreateDirectory(Path.Combine(DataFolderPath, "TOKElfTool"));
            }

            // Read recently opened files
            if (File.Exists(HistoryPath))
            {
                recentlyOpenedFiles = File.ReadAllLines(HistoryPath).ToList();
                RegenerateRecentlyOpened();
            }
        }

        ~MainWindow()
        {
            compressor.Dispose();
            decompressor.Dispose();
        }

        private static readonly string DataFolderPath = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create);
        private static readonly string HistoryPath = Path.Combine(DataFolderPath, "TOKElfTool/file_history.txt");

        private GameDataType loadedDataType = GameDataType.NPC;

        private bool hasUnsavedChanges;

        private void InitializeObjectsPanel<T>(Element<T>[] objects, string objectName)
        {
            EmptyLabel.Visibility = Visibility.Collapsed;
            ScrollViewer.Visibility = Visibility.Visible;

            for (int i = 0; i < objects.Length; i++)
            {
                object currentObject = objects[i].value;

                ObjectEditControl expander = new ObjectEditControl(currentObject, $"{objectName} {i}");
                
                expander.RemoveButtonClick += RemoveButton_OnClick;
                expander.DuplicateButtonClick += DuplicateButton_OnClick;

                expander.ValueChanged += (sender, args) => hasUnsavedChanges = true;

                ObjectTabPanel.Children.Add(expander);
            }

        }

        private void FixExpanderNames()
        {
            for (int i = 1; i < ObjectTabPanel.Children.Count; i++)
            {
                ObjectEditControl expander = (ObjectEditControl)ObjectTabPanel.Children[i];
                expander.Header = $"{loadedDataType} {i - 1}";
            }
        }

        /// <summary>
        /// This field is for when it wants to add an expander, i.e. duplicate one, but there is no expander left
        /// </summary>
        private ObjectEditControl duplicateExpander;

        private async void DuplicateButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button duplicateButton = (Button)sender;
            Grid grid = (Grid)duplicateButton.Parent;
            Expander expander = (Expander)grid.Parent;
            ObjectEditControl objectEditControl = (ObjectEditControl)expander.Parent;

            ObjectEditControl clone = null;
            await Dispatcher.InvokeAsync(() => clone = objectEditControl.XamlClone());

            clone.IsExpanded = false;
            ObjectTabPanel.Children.Insert(ObjectTabPanel.Children.IndexOf(objectEditControl), clone);

            hasUnsavedChanges = true;

            FixExpanderNames();
        }
        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button duplicateButton = (Button)sender;
            Grid grid = (Grid)duplicateButton.Parent;
            ObjectEditControl expander = (ObjectEditControl)grid.Parent;

            bool? result = MyMessageBox.Show(this, $"Are you sure you want to delete this {loadedDataType}?", "TOK ELF Editor", MessageBoxResult.Yes);
            if (result == true)
            {
                ObjectTabPanel.Children.Remove(expander);
                duplicateExpander = expander;

                hasUnsavedChanges = true;

                FixExpanderNames();
            }
        }


        private readonly List<string> recentlyOpenedFiles = new List<string>();
        private void AddRecentlyOpened(string filepath)
        {
            recentlyOpenedFiles.Remove(filepath);
            recentlyOpenedFiles.Add(filepath);

            // Save to file
            File.WriteAllText(HistoryPath, string.Join("\n", recentlyOpenedFiles));

            RegenerateRecentlyOpened();
        }

        private void RegenerateRecentlyOpened()
        {
            // Regenerate menu item
            OpenRecentItem.Items.Clear();
            for (int i = recentlyOpenedFiles.Count - 1; i >= 0; i--)
            {
                string name = recentlyOpenedFiles[i];

                MenuItem menuItem = new MenuItem
                {
                    Header = name,
                };
                OpenRecentItem.Items.Add(menuItem);
            }
        }

        private void CommandBinding_New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        private void CommandBinding_New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("New not supported currently");
        }

        private void CommandBinding_Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void RemoveAllObjects()
        {
            // Reverse loop that ignores first element (Add/remove controls)
            for (int i = ObjectTabPanel.Children.Count - 1; i >= 1; i--)
            {
                ObjectTabPanel.Children.RemoveAt(i);
            }
        }

        private ElfBinary<NPC> loadedBinary;

        private string containingFolderPath = "";

        private async void CommandBinding_Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            loadedDataType = GameDataType.NPC;

            OpenFileDialog dialog = new OpenFileDialog
            {
                FileName = "dispos_Npc.elf",
                DefaultExt = ".elf",
                Filter = "ELF Files (*.elf)|*.elf|Zstd Compressed ELF Files (*.elf.zst)|*.elf.zst"
            };
            bool? result = dialog.ShowDialog(this);
            if (result == true)
            {
                Title = $"{dialog.FileName} - TOK ELF Editor";
                RemoveAllObjects();
                if (dialog.FilterIndex == 2)
                {
                    byte[] input = File.ReadAllBytes(dialog.FileName);
                    byte[] decompessed = decompressor.Unwrap(input);
                    MemoryStream memoryStream = new MemoryStream(decompessed)
                    {
                        Position = 0,
                    };
                    BinaryReader reader = new BinaryReader(memoryStream);

                    loadedBinary = await Task.Run(() => ElfParser.ParseFile<NPC>(reader, GameDataType.NPC));
                }
                else
                    loadedBinary = await Task.Run(() => ElfParser.ParseFile<NPC>(dialog.FileName, GameDataType.NPC));
                fileSavePath = null;
                containingFolderPath = Path.GetDirectoryName(dialog.FileName) ?? @"C:\Users";
                AddRecentlyOpened(dialog.FileName);
                await Dispatcher.InvokeAsync(() => InitializeObjectsPanel(loadedBinary.Data.ToArray(), "NPC"));
            }
        }
        private string ShowOptionalSaveDialog(string savePath)
        {
            if (savePath == null)
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    FileName = "dispos_Npc.elf",
                    DefaultExt = ".elf",
                    Filter = "ELF Files (*.elf)|*.elf|Zstd Compressed ELF Files (*.elf.zst)|*.elf.zst",
                };
                bool? result = dialog.ShowDialog(this);
                if (result == true)
                    savePath = dialog.FileName;
            }
            return savePath;
        }


        private void CommandBinding_Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = loadedBinary != null;
        }

        private string fileSavePath;
        private readonly Compressor compressor = new Compressor();
        private readonly Decompressor decompressor = new Decompressor();

        private void CommandBinding_Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<Element<NPC>> objects = CollectObjects(ObjectTabPanel);

            #region Logging
            Trace.WriteLine("NPCs:");
            Trace.Indent();
            foreach (var item in objects)
            {
                Trace.WriteLine(item);
            }
            Trace.Unindent();
            #endregion

            loadedBinary.Data = objects;

            byte[] serialized = ElfSerializer.SerializeBinary(loadedBinary, loadedDataType);

            fileSavePath = ShowOptionalSaveDialog(fileSavePath);
            if (fileSavePath != null)
            {
                try
                {
                    hasUnsavedChanges = false;
                    File.WriteAllBytes(fileSavePath, fileSavePath.EndsWith(".zst") || fileSavePath.EndsWith(".zstd") ? compressor.Wrap(serialized) : serialized);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception);
                    MessageBox.Show(this, "Couldn't save the file. Maybe it's in use or doesn't exist", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void CommandBinding_SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = loadedBinary != null;
        }

        private void CommandBinding_SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<Element<NPC>> objects = CollectObjects(ObjectTabPanel);

            #region Logging
            Trace.WriteLine("NPCs:");
            Trace.Indent();
            foreach (var item in objects)
            {
                Trace.WriteLine(item);
            }
            Trace.Unindent();
            #endregion

            loadedBinary.Data = objects;

            byte[] serialized = ElfSerializer.SerializeBinary(loadedBinary, loadedDataType);

            fileSavePath = ShowOptionalSaveDialog(null);
            if (fileSavePath != null)
            {
                try
                {
                    hasUnsavedChanges = false;
                    File.WriteAllBytes(fileSavePath, fileSavePath.EndsWith(".zst") || fileSavePath.EndsWith(".zstd") ? compressor.Wrap(serialized) : serialized);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception);
                    MessageBox.Show(this, "Couldn't save the file. Maybe it's in use or doesn't exist", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private List<Element<NPC>> CollectObjects(Panel objectPanel)
        {
            List<Element<NPC>> objects = new List<Element<NPC>>();

            // go through all NPC's
            for (int i = 0; i < objectPanel.Children.Count; i++)
            {
                if (i == 0)
                    continue;

                ObjectEditControl expander = (ObjectEditControl)objectPanel.Children[i];


                objects.Add(new Element<NPC>((NPC)CollectObject(expander)));
            }

            return objects;
        }

        private object CollectObject(ObjectEditControl expander)
        {
            Grid grid = (Grid)expander.Content;

            Trace.WriteLine(expander);

            // go through all property controls
            object currentNpc = new NPC();
            string propertyName = "";
            Type propertyType = null;
            object propertyValue = null;

            for (int j = 0; j < grid.Children.Count; j++)
            {
                UIElement child = grid.Children[j];

                // Ignore first child (Control buttons)
                if (j < 2)
                    continue;

                // key label
                if (j % 2 == 0)
                {
                    propertyName = ((TextBlock)child).Text;
                    propertyType = typeof(NPC).GetField(propertyName).FieldType;

                    continue;
                }

                // checkbox
                if (propertyType == typeof(bool))
                {
                    CheckBox checkBox = (CheckBox)child;
                    propertyValue = checkBox.IsChecked;
                    Trace.WriteLine($"{propertyName}: {propertyType.Name} = {propertyValue} (from checkbox)");

                    continue;
                }

                // dropdown


                // TextBox
                TextBox textBox = (TextBox)child;
                string text = textBox.Text;
                switch (propertyType?.Name)
                {
                    case "String":
                        propertyValue = text.StartsWith("\"") && text.EndsWith("\"")
                            ? text.Substring(1, text.Length - 2)
                            : null;
                        break;
                    case "Vector3":
                        propertyValue = Vector3.FromString(text);
                        break;
                    case "Int32":
                        _ = int.TryParse(text, out int propertyValueInt);
                        propertyValue = propertyValueInt;
                        break;
                    case "Int64":
                        long.TryParse(text, out long propertyValueLong);
                        propertyValue = propertyValueLong;
                        break;
                    case "Single":
                        string floatString = text.EndsWith("f") ? text.Substring(0, text.Length - 1) : text;
                        float.TryParse(floatString, out float propertyValueFloat);
                        propertyValue = propertyValueFloat;
                        break;
                    case "Double":
                        double.TryParse(text, out double propertyValueDouble);
                        propertyValue = propertyValueDouble;
                        break;
                }

                Trace.WriteLine($"{propertyName}: {propertyType?.Name} = {propertyValue} (original {text})");

                typeof(NPC).GetField(propertyName).SetValue(currentNpc, propertyValue);

            }

            return currentNpc;
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "TOK ELF Tool\nMade by Darxoon", "TOK ELF Tool", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            e.Handled = true;
        }

        private void MenuItem_OpenRepo_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Darxoon/TOKElfTool/"));
            e.Handled = true;
        }

        private void MenuItem_OpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(containingFolderPath);
            e.Handled = true;
        }

        private void MenuItem_Decrypt_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                FileName = "dispos_Npc.elf.zst",
                DefaultExt = ".zst",
                Filter = "ZSTD Compressed File (*.zst; *.zstd)|*.zst;*.zstd|All Files (*.*)|*",
            };
            bool? result = openFileDialog.ShowDialog(this);
            if (result == true)
            {

                byte[] input;
                try
                {
                    input = File.ReadAllBytes(openFileDialog.FileName);
                }
                catch
                {
                    MessageBox.Show(this, "Couldn't read file. Maybe it's opened by another program or doesn't exist",
                        "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }

                byte[] decompressed = decompressor.Unwrap(input);

                string filename = Path.GetFileName(openFileDialog.FileName);
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = filename.EndsWith(".zst")
                        ? filename.Substring(0, filename.Length - ".zst".Length)
                        : filename.EndsWith(".zstd")
                            ? filename.Substring(0, filename.Length - ".zstd".Length)
                            : filename + ".dec",
                    DefaultExt = "",
                    Filter = "All Files (*.*)|*",
                    OverwritePrompt = true,
                };
                bool? saveResult = saveFileDialog.ShowDialog(this);
                if (saveResult == true)
                {
                    try
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, decompressed);
                    }
                    catch
                    {
                        MessageBox.Show(this, "Couldn't save the file. Maybe it's opened by another program",
                            "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    }
                }
            }
        }

        private void MenuItem_Encrypt_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                FileName = "",
                DefaultExt = "",
                Filter = "All Files (*.*)|*",
            };
            bool? openResult = openFileDialog.ShowDialog(this);
            if (openResult == true)
            {
                Trace.WriteLine(openFileDialog.FileName);
                byte[] input;
                try
                {
                    input = File.ReadAllBytes(openFileDialog.FileName);
                }
                catch
                {
                    MessageBox.Show(this, "Couldn't read file. Maybe it's opened by another program or doesn't exist",
                        "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }

                byte[] compressed = compressor.Wrap(input);

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = Path.GetFileName(openFileDialog.FileName) + ".zst",
                    DefaultExt = ".zst",
                    Filter = "ZSTD Compressed File (*.zst; *.zstd)|*.zst;*.zstd|All Files (*.*)|*",
                    OverwritePrompt = true,
                };
                bool? saveResult = saveFileDialog.ShowDialog(this);
                if (saveResult == true)
                {
                    try
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, compressed);
                    }
                    catch
                    {
                        MessageBox.Show(this, "Couldn't save the file. Maybe it's opened by another program",
                            "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    }
                }
            }
        }

        private void Button_RemoveAllObjects_OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (ObjectTabPanel.Children.Count == 0)
                return;
            bool? result = MyMessageBox.Show(this, "Are you sure you want to remove all objects?", "TOK ELF Editor",
                MessageBoxResult.Yes);
            if (result == true)
            {
                duplicateExpander = (ObjectEditControl)ObjectTabPanel.Children.Last();
                RemoveAllObjects();
            }
        }

        private async void Button_AddObject_OnClick(object sender, RoutedEventArgs e)
        {
            ObjectEditControl clone = null;
            e.Handled = true;
            await Dispatcher.InvokeAsync(() => clone = (ObjectEditControl)(ObjectTabPanel.Children.Count > 1
                ? ObjectTabPanel.Children.Last()
                : duplicateExpander).XamlClone());
            clone.IsExpanded = true;
            ObjectTabPanel.Children.Add(clone);
            FixExpanderNames();
        }
        

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (hasUnsavedChanges)
            {
                bool? result = MyMessageBox.Show(this, "You have unsaved changes. Do you want to quit?",
                    "TOK ELF Editor",
                    MessageBoxResult.No);

                e.Cancel = !result ?? false;
            }
        }
    }
}
