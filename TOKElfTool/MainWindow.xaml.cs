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
using ElfLib.CustomDataTypes.Registry;
using Ookii.Dialogs.Wpf;
using TOKElfTool.ProgressReports;
using TOKElfTool.Search;
using ZstdNet;
using FieldInfo = System.Reflection.FieldInfo;
using UIElement = System.Windows.UIElement;

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

            editors = new List<EditorPanel>();
            
            EmptyLabel.Visibility = Visibility.Visible;
            tabControl.Visibility = Visibility.Collapsed;

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
                EmptyLabel.UpdateList(4, ((IEnumerable<string>)recentlyOpenedFiles).Reverse().ToArray());
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

        private List<EditorPanel> editors;

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
                string path = recentlyOpenedFiles[i];

                MenuItem menuItem = new MenuItem
                {
                    Header = Util.ShortenPath(path),
                };
                menuItem.Click += async (sender, args) =>
                {
                    string filename = Path.GetFileName(path);

                    GameDataType? type = ObjectTypeSelector.Show(this, filename.Split('.')[0]);

                    if (type is null)
                        return;

                    OpenFile(path, (GameDataType)type, filename.EndsWith(".zst"));
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
                GameDataType? type = ObjectTypeSelector.Show(this, simpleFileName);

                if (type is null)
                    return;

                bool isCompressed = dialog.FilterIndex == 3 || dialog.FilterIndex == 1 && dialog.SafeFileName.EndsWith(".elf.zst");
                OpenFile(dialog.FileName, (GameDataType)type, isCompressed);
            }
        }


        private void OpenFile(string filename, GameDataType type, bool isCompressed)
        {
            // Prepare UI for loading
            Title = $"{Util.ShortenPath(filename)} - TOK ELF Editor";
            EmptyLabel.Visibility = Visibility.Collapsed;
            LoadingLabel.Visibility = Visibility.Visible;
            tabControl.Visibility = Visibility.Collapsed;

            statusLabel.Text = "Loading ELF file...";

            // TODO: force redraw
            
            LoadDataType(type);

            using BinaryReader reader = CreateFileReader(isCompressed, filename);
            ElfBinary<object> binary = LoadBinary(reader);

            loadedBinary = binary;
            hasUnsavedChanges = false;
            fileSavePath = null;
            containingFolderPath = Path.GetDirectoryName(filename) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // TODO: generate editor panel
            
            foreach (EditorPanel editor in editors)
                editor.searchBar.HasIndexed = false;

            AddRecentlyOpened(filename);
            
            openContainingItem.IsEnabled = true;
            collapseAllObjectsItem.IsEnabled = true;
            expandAllObjectsItem.IsEnabled = true;

            LoadingLabel.Visibility = Visibility.Collapsed;
            tabControl.Visibility = Visibility.Visible;
            statusLabel.Text = "Loaded file";
        }

        private void LoadDataType(GameDataType type)
        {
            loadedDataType = type;
            loadedStructType = type switch
            {
                GameDataType.NPC => typeof(NPC),
                GameDataType.Mobj => typeof(Mobj),
                GameDataType.Aobj => typeof(Aobj),
                GameDataType.BShape => typeof(BShape),
                GameDataType.Item => typeof(Item),
                GameDataType.Maplink => typeof(MaplinkNode),
                GameDataType.DataNpc => typeof(NpcType),
                GameDataType.DataItem => typeof(ItemType),
                GameDataType.DataNpcModel => typeof(NpcModel),
                GameDataType.None => null,
                _ => throw new Exception("Data type currently not supported")
            };
        }


        private ElfBinary<object> LoadBinary(BinaryReader reader)
        {
            ElfBinary<object> binary;

            try
            {
                binary = ElfParser.ParseFile<object>(reader, loadedDataType);
            }
            catch (ElfContentNotFoundException)
            {
                MyMessageBox.Show(this, "The file is empty.", "TOK ELF Editor", MessageBoxResult.OK,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                // Load empty.elf
                Stream emptyElfStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TOKElfTool.empty.elf");

                return LoadBinary(new BinaryReader(emptyElfStream));
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


        private (string savePath, bool isCompressed) ShowOptionalSaveDialog(string savePath)
        {
            bool isCompressed = savePath?.EndsWith(".zst") ?? false;

            if (savePath == null)
            {
                VistaSaveFileDialog dialog = new VistaSaveFileDialog()
                {
                    FileName = loadedDataType switch
                    {
                        GameDataType.Maplink => "maplink.elf",
                        GameDataType.NPC => "dispos_Npc.elf",
                        GameDataType.DataNpc => "data_npc.elf",
                        GameDataType.DataItem => "data_item.elf",
                        _ => $"dispos_{loadedStructType.Name}.elf",
                    },
                    //DefaultExt = ".elf",
                    Filter = "ELF Files (*.elf)|*.elf|Zstd Compressed ELF Files (*.elf.zst)|*.elf.zst",
                };
                bool? result = dialog.ShowDialog(this);
                if (result == true)
                    savePath = dialog.FileName;
            }
            return (savePath, isCompressed);
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
            fileSavePath = ShowOptionalSaveDialog(fileSavePath).savePath;
            if (fileSavePath != null)
            {
                statusLabel.Text = "Saving file...";

                hasUnsavedChanges = false;

                loadedBinary.Data[0] = CollectObjects(editors[0].objectTabPanel.Children);

                SavePopupWindow popup = new SavePopupWindow(loadedBinary, fileSavePath, loadedDataType)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                };
                popup.ShowDialog();

                statusLabel.Text = "Saved file";
            }
        }
        private void CommandBinding_SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = loadedBinary != null;
        }

        private void CommandBinding_SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            (string savePath, bool isCompressed) = ShowOptionalSaveDialog(null);
            fileSavePath = savePath;

            if (savePath != null)
            {
                statusLabel.Text = "Saving file...";

                hasUnsavedChanges = false;

                loadedBinary.Data[0] = CollectObjects(editors[0].objectTabPanel.Children);

                if (loadedDataType == GameDataType.Maplink)
                    loadedBinary.Data[ElfType.MaplinkHeader][0] = loadedBinary.Data[0].PopBack();

                if (savePath.EndsWith(".elf.zst.elf.zst"))
                    savePath = savePath.Substring(0, savePath.Length - ".elf.zst".Length);
                if (isCompressed && !savePath.EndsWith(".zst"))
                    savePath += ".zst";

                SavePopupWindow popup = new SavePopupWindow(loadedBinary, savePath, loadedDataType)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                };
                popup.ShowDialog();

                statusLabel.Text = "Saved file";
            }
        }

        private List<Element<object>> CollectObjects(UIElementCollection children)
        {
            if (searchResultControls != null)
                UndoSearch();

            return null;
        }


        private object CollectObject(ObjectEditControl objectEditControl)
        {
            objectEditControl.Generate();

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

        private object CollectMaplinkHeaderObject(ObjectEditControl objectEditControl)
        {
            objectEditControl.Generate();
            
            Grid grid = objectEditControl.Grid;

            // go through all property controls
            Type headerType = typeof(MaplinkHeader);
            object currentObject = new MaplinkHeader();

            string propertyName = "";
            Type propertyType = null;

            for (int j = 0; j < grid.Children.Count; j++)
            {
                UIElement child = grid.Children[j];

                // key label
                if (j % 2 == 0)
                {
                    propertyName = ((TextBlock)child).Text;
                    propertyType = headerType.GetField(propertyName).FieldType;

                    continue;
                }

                if (propertyType is null)
                    continue;

                object propertyValue = ReadFromControl(propertyType, child);

                Trace.WriteLine($"{propertyName}: {propertyType?.Name} = {propertyValue}");

                headerType.GetField(propertyName).SetValue(currentObject, propertyValue);

            }

            return currentObject;
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
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog
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


        private void MenuItem_DecryptAll_OnClick(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog openDialog = new VistaFolderBrowserDialog
            {
                Description = "Select Folder to Decompress",
                UseDescriptionForTitle = true,
            };
            bool? result = openDialog.ShowDialog(this);
            if (result != true)
                return;

            VistaFolderBrowserDialog saveDialog = new VistaFolderBrowserDialog
            {
                Description = "Select Output Folder",
                UseDescriptionForTitle = true,
            };
            result = saveDialog.ShowDialog(this);
            if (result != true)
                return;

            statusLabel.Text = "Decompressing folder...";

            ZstdFolderProgressPopupWindow window = new ZstdFolderProgressPopupWindow
            {
                TargetDir = openDialog.SelectedPath,
                OutputDir = saveDialog.SelectedPath,
                Method = ZstdMethod.Decompress,
            };
            window.ShowDialog();

            statusLabel.Text = "Done bulk compressing";
        }

        private void MenuItem_EncryptAll_OnClick(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog openDialog = new VistaFolderBrowserDialog
            {
                Description = "Select Folder to Compress",
                UseDescriptionForTitle = true,
            };
            bool? result = openDialog.ShowDialog(this);
            if (result != true)
                return;

            VistaFolderBrowserDialog saveDialog = new VistaFolderBrowserDialog
            {
                Description = "Select Output Folder",
                UseDescriptionForTitle = true,
            };
            result = saveDialog.ShowDialog(this);
            if (result != true)
                return;

            statusLabel.Text = "Compressing folder...";

            ZstdFolderProgressPopupWindow window = new ZstdFolderProgressPopupWindow
            {
                TargetDir = openDialog.SelectedPath,
                OutputDir = saveDialog.SelectedPath,
                Method = ZstdMethod.Compress,
            };
            window.ShowDialog();

            statusLabel.Text = "Done bulk compressing";
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

        private async void EmptyLabel_OnEntryClick(object sender, MouseButtonEventArgs e)
        {
            string path = recentlyOpenedFiles[recentlyOpenedFiles.Count - 1 - (int)sender];
            string filename = Path.GetFileName(path);

            GameDataType? type = ObjectTypeSelector.Show(this, filename.Split('.')[0]);

            if (type is null)
                return;

            OpenFile(path, (GameDataType)type, filename.EndsWith(".zst"));
        }

        private void CollapseAllObjects_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void ExpandAllObjects_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private SearchIndex searchIndex;

        private ObjectEditControl[] searchResultControls = null;

        private void UndoSearch()
        {
            
        }
    }
}
