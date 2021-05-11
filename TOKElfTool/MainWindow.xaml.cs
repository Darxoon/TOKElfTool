using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ElfLib;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using ElfLib.CustomDataTypes;
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
                EmptyLabel.UpdateList(4, recentlyOpenedFiles.ToArray());
            }
            else
            {
                EmptyLabel.UpdateList(0, new string[0]);
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

        private GameDataType loadedDataType;
        private Type loadedStructType;

        private bool hasUnsavedChanges;

        private void InitializeObjectsPanel<T>(List<Element<T>>[] objects, string objectName)
        {
            EmptyLabel.Visibility = Visibility.Collapsed;
            ScrollViewer.Visibility = Visibility.Visible;

            for (int j = 0; j < objects.Length; j++)
            {
                List<Element<T>> sectionObjects = objects[j];

                for (int i = 0; i < sectionObjects.Count; i++)
                {
                    object currentObject = sectionObjects[i].value;

                    ObjectEditControl expander = new ObjectEditControl(currentObject, $"{objectName} {i}");

                    expander.RemoveButtonClick += RemoveButton_OnClick;
                    expander.DuplicateButtonClick += DuplicateButton_OnClick;

                    expander.ValueChanged += (sender, args) => hasUnsavedChanges = true;

                    ObjectTabPanel.Children.Add(expander);
                }
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
            await Dispatcher.InvokeAsync(() => clone = objectEditControl.Clone());

            clone.IsExpanded = false;
            ObjectTabPanel.Children.Insert(ObjectTabPanel.Children.IndexOf(objectEditControl), clone);

            hasUnsavedChanges = true;

            FixExpanderNames();
        }
        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button duplicateButton = (Button)sender;
            Grid grid = (Grid)duplicateButton.Parent;
            Expander expander = (Expander)grid.Parent;
            ObjectEditControl objectEditControl = (ObjectEditControl)expander.Parent;

            bool? result = MyMessageBox.Show(this, $"Are you sure you want to delete this {loadedDataType}?", "TOK ELF Editor", MessageBoxResult.Yes);
            if (result == true)
            {
                ObjectTabPanel.Children.Remove(objectEditControl);
                duplicateExpander = objectEditControl;

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
                    Header = Util.ShortenPath(name),
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

        private ElfBinary<object> loadedBinary;

        private string containingFolderPath = "";

        private async void CommandBinding_Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                FileName = "dispos_Npc.elf",
                DefaultExt = ".elf",
                Filter = "All ELF Files (*.elf; *.elf.zst)|*.elf;*.elf.zst|ELF Files (*.elf)|*.elf|Zstd Compressed ELF Files (*.elf.zst)|*.elf.zst",
            };
            bool? result = dialog.ShowDialog(this);
            if (result == true)
            {
                string simpleFileName = dialog.SafeFileName.Split('.')[0];
                GameDataType? type = ObjectTypeSelector.Show(simpleFileName);

                if (type is null)
                    return;

                bool isCompressed = dialog.FilterIndex == 3 || dialog.FilterIndex == 1 && dialog.SafeFileName.EndsWith(".elf.zst");
                await OpenFile(dialog.FileName, (GameDataType)type, isCompressed);
            }
        }

        private async Task<ElfBinary<object>> OpenFile(string filename, GameDataType type, bool isCompressed)
        {
            Title = $"{Util.ShortenPath(filename)} - TOK ELF Editor";
            EmptyLabel.Visibility = Visibility.Collapsed;
            LoadingLabel.Visibility = Visibility.Visible;
            ScrollViewer.Visibility = Visibility.Collapsed;

            statusLabel.Text = "Loading ELF file...";

            LoadDataType(type);
            RemoveAllObjects();

            ElfBinary<object> binary = await LoadBinary(isCompressed, filename);

            if (binary is null)
                return null;

            Trace.WriteLine("symbol table:");
            Trace.WriteLine(string.Join(", ", binary.SymbolTable.Select(x => x.Name)));

            loadedBinary = binary;

            fileSavePath = null;
            containingFolderPath = Path.GetDirectoryName(filename) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            AddRecentlyOpened(filename);

            // initialize objects panel
            await Dispatcher.InvokeAsync(() => InitializeObjectsPanel<object>(loadedBinary.Data, loadedDataType.ToString()));

            LoadingLabel.Visibility = Visibility.Collapsed;
            ScrollViewer.Visibility = Visibility.Visible;
            statusLabel.Text = "Loaded file";

            return binary;
        }

        private void LoadDataType(GameDataType type)
        {
            loadedDataType = type;
            switch (type)
            {
                case GameDataType.NPC:
                    loadedStructType = typeof(NPC);
                    break;
                case GameDataType.Mobj:
                    loadedStructType = typeof(Mobj);
                    break;
                case GameDataType.Aobj:
                    loadedStructType = typeof(Aobj);
                    break;
                case GameDataType.BShape:
                    loadedStructType = typeof(BShape);
                    break;
                case GameDataType.Item:
                    loadedStructType = typeof(Item);
                    break;
                case GameDataType.None:
                    loadedStructType = null;
                    break;
                default:
                    throw new Exception("Data type currently not supported");
            }
        }


        private async Task<ElfBinary<object>> LoadBinary(bool isCompressed, string filename)
        {
            BinaryReader reader = CreateFileReader(isCompressed, filename);

            return await LoadBinary(reader);
        }

        private async Task<ElfBinary<object>> LoadBinary(BinaryReader reader)
        {
            ElfBinary<object> binary;

            try
            {
                binary = await Task.Run(() => ElfParser.ParseFile<object>(reader, loadedDataType));
            }
            catch (ElfContentNotFoundException)
            {
                MyMessageBox.Show(this, "The file is empty.", "TOK ELF Editor", MessageBoxResult.OK,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                // Load empty.elf
                Stream emptyElfStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TOKElfTool.empty.elf");

                object newObject = Activator.CreateInstance(loadedStructType);
                duplicateExpander = new ObjectEditControl(newObject, "");

                return await LoadBinary(new BinaryReader(emptyElfStream));
            }
            // don't catch this during debug so it can be caught by the debugger
#if !DEBUG
            catch (ElfParseException elfException)
            {
                MyMessageBox.Show(this, $"An error occurred: \"{elfException}\"\n{elfException.StackTrace}", 
                    "TOK ELF Editor", MessageBoxResult.OK, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
#endif
            return binary;
        }

        private BinaryReader CreateFileReader(bool isCompressed, string filename)
        {
            if (isCompressed)
            {
                byte[] input = File.ReadAllBytes(filename);
                byte[] decompessed = decompressor.Unwrap(input);
                MemoryStream memoryStream = new MemoryStream(decompessed)
                {
                    Position = 0,
                };

                return new BinaryReader(memoryStream);
            }
            else
            {
                FileStream input = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read,
                    (int)new FileInfo(filename).Length);

                return new BinaryReader(input);
            }
        }


        private string ShowOptionalSaveDialog(string savePath)
        {
            if (savePath == null)
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    FileName = $"dispos_{(loadedStructType == typeof(NPC) ? "Npc" : nameof(loadedStructType))}.elf",
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

            fileSavePath = ShowOptionalSaveDialog(fileSavePath);
            if (fileSavePath != null)
            {
                statusLabel.Text = "Saving file...";
                try
                {
                    hasUnsavedChanges = false;

                    List<Element<object>> objects = CollectObjects(ObjectTabPanel);
                    loadedBinary.Data[0] = objects;
                    byte[] serialized = ElfSerializer.SerializeBinary(loadedBinary, loadedDataType, true);

                    File.WriteAllBytes(fileSavePath, fileSavePath.EndsWith(".zst") || fileSavePath.EndsWith(".zstd") ? compressor.Wrap(serialized) : serialized);
                    statusLabel.Text = "Saved file";
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception);
                    MessageBox.Show(this, "Couldn't save the file. Maybe it's in use or doesn't exist", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                    statusLabel.Text = "Failed to save file";
                }
            }
        }
        private void CommandBinding_SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = loadedBinary != null;
        }

        private void CommandBinding_SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {

            fileSavePath = ShowOptionalSaveDialog(null);
            if (fileSavePath != null)
            {
                statusLabel.Text = "Saving file...";
                try
                {
                    hasUnsavedChanges = false;

                    List<Element<object>> objects = CollectObjects(ObjectTabPanel);
                    loadedBinary.Data[0] = objects;
                    byte[] serialized = ElfSerializer.SerializeBinary(loadedBinary, loadedDataType, true);

                    File.WriteAllBytes(fileSavePath, fileSavePath.EndsWith(".zst") || fileSavePath.EndsWith(".zstd") ? compressor.Wrap(serialized) : serialized);
                    statusLabel.Text = "Saved file";
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception);
                    MessageBox.Show(this, "Couldn't save the file. Maybe it's in use or doesn't exist", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                    statusLabel.Text = "Failed to save file";
                }
            }
        }

        private List<Element<object>> CollectObjects(Panel objectPanel)
        {
            List<Element<object>> objects = new List<Element<object>>();

            // go through all NPC's
            for (int i = 0; i < objectPanel.Children.Count; i++)
            {
                if (i == 0)
                    continue;

                ObjectEditControl expander = (ObjectEditControl)objectPanel.Children[i];


                objects.Add(new Element<object>(CollectObject(expander)));
            }

            return objects;
        }

        private object CollectObject(ObjectEditControl objectEditControl)
        {
            Grid grid = objectEditControl.Grid;

            Trace.WriteLine(objectEditControl);

            // go through all property controls
            object currentObjects = Activator.CreateInstance(loadedStructType);

            string propertyName = "";
            Type propertyType = null;

            for (int j = 0; j < grid.Children.Count; j++)
            {
                UIElement child = grid.Children[j];

                // key label
                if (j % 2 == 0)
                {
                    propertyName = ((TextBlock)child).Text;
                    propertyType = loadedStructType.GetField(propertyName).FieldType;

                    continue;
                }

                if (propertyType is null)
                    continue;

                object propertyValue = ReadFromControl(propertyType, child);

                Trace.WriteLine($"{propertyName}: {propertyType?.Name} = {propertyValue}");

                loadedStructType.GetField(propertyName).SetValue(currentObjects, propertyValue);

            }

            return currentObjects;
        }

        private object ReadFromControl(Type propertyType, UIElement child)
        {
            // checkbox
            if (propertyType == typeof(bool))
            {
                CheckBox checkBox = (CheckBox)child;
                //Trace.WriteLine($"{propertyName}: {propertyType.Name} = {propertyValue} (from checkbox)");

                return checkBox.IsChecked;
            }

            // dropdown
            if (propertyType.BaseType == typeof(Enum))
            {
                ComboBox comboBox = (ComboBox)child;
                FieldInfo[] enumFields = propertyType.GetFields()
                    .Where(value => value.IsStatic)
                    .ToArray();

                FieldInfo selectedField = enumFields[comboBox.SelectedIndex];
                int selectedFieldValue = (int)selectedField.GetValue(null);

                //Trace.WriteLine($"~~~~~~~~~~ {propertyValue}");
                return selectedFieldValue;
            }

            // TextBox
            TextBox textBox = (TextBox)child;
            string text = textBox.Text;
            switch (propertyType.Name)
            {
                case "String":
                    return text.StartsWith("\"") && text.EndsWith("\"")
                        ? text.Substring(1, text.Length - 2)
                        : null;
                case "Vector3":
                    return Vector3.FromString(text);
                case "Byte":
                    _ = byte.TryParse(text, out byte propertyValueByte);
                    return propertyValueByte;
                case "Int32":
                    _ = int.TryParse(text, out int propertyValueInt);
                    return propertyValueInt;
                case "Int64":
                    long.TryParse(text, out long propertyValueLong);
                    return propertyValueLong;
                case "Single":
                    string floatString = text.EndsWith("f") ? text.Substring(0, text.Length - 1) : text;
                    float.TryParse(floatString, out float propertyValueFloat);
                    return propertyValueFloat;
                case "Double":
                    double.TryParse(text, out double propertyValueDouble);
                    return propertyValueDouble;
            }

            throw new Exception("Couldn't read the property value");
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "TOK ELF Tool\nMade by Darxoon", "TOK ELF Tool", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            e.Handled = true;
        }

        private void MenuItem_OpenRepo_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Darxoon/TOKElfTool/"));
            statusLabel.Text = "Opened online repository";
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
                statusLabel.Text = "Decompressing file...";

                byte[] input;
                try
                {
                    input = File.ReadAllBytes(openFileDialog.FileName);
                }
                catch
                {
                    MyMessageBox.Show(this, "Couldn't read file. Maybe it's opened by another program or doesn't exist",
                        "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    statusLabel.Text = "Failed to decompress file";
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
                        statusLabel.Text = "Decompressed file";
                    }
                    catch
                    {
                        MessageBox.Show(this, "Couldn't save the file. Maybe it's opened by another program",
                            "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        statusLabel.Text = "Failed to decompress file";
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
                statusLabel.Text = "Compressing file...";
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
                    statusLabel.Text = "Failed to compress file";
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
                        statusLabel.Text = "Compressed file";
                    }
                    catch
                    {
                        MessageBox.Show(this, "Couldn't save the file. Maybe it's opened by another program",
                            "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        statusLabel.Text = "Failed to compress file";
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
            await Dispatcher.InvokeAsync(() =>
            {
                clone = ObjectTabPanel.Children.Count > 1
                    ? ((ObjectEditControl)ObjectTabPanel.Children.Last()).Clone()
                    : duplicateExpander.Clone();
            });
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

        private void EmptyLabel_OnEntryClick(object sender, MouseButtonEventArgs e)
        {
            string path = recentlyOpenedFiles[(int)sender];
            string filename = Path.GetFileName(path);

            GameDataType? type = ObjectTypeSelector.Show(filename.Split('.')[0]);

            if (type is null)
                return;

            Dispatcher.InvokeAsync(() => OpenFile(path, (GameDataType)type, filename.EndsWith(".zst")));
        }
    }
}
