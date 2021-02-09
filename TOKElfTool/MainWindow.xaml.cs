using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ElfLib;
using System.Text.RegularExpressions;
using System.Globalization;
using stStackPanel = AutoGrid.StackPanel;
using stAutoGrid = AutoGrid.AutoGrid;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        private static readonly FontFamily ConsolasFontFamily = new FontFamily("Consolas");

        private static readonly string DataFolderPath = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create);
        private static readonly string HistoryPath = Path.Combine(DataFolderPath, "TOKElfTool/file_history.txt");

        private static readonly NumberFormatInfo Nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = "."
        };

        private GameDataType loadedDataType = GameDataType.NPC;

        private void InitializeObjectsPanel<T>(Element<T>[] objects, string objectName)
        {
            EmptyLabel.Visibility = Visibility.Collapsed;
            ScrollViewer.Visibility = Visibility.Visible;

            for (int objectIndex = 0; objectIndex < objects.Length; objectIndex++)
            {
                object currentObject = objects[objectIndex].value;

                Expander expander = new Expander
                {
                    Header = $"{objectName} {objectIndex}"
                };

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                for (int j = 0; j < 100; j++)
                {
                    grid.RowDefinitions.Add(new RowDefinition());
                }

                Button removeButton = new Button
                {
                    Margin = new Thickness(5),
                    Content = "Remove",
                    Tag = objectIndex
                };
                Grid.SetColumn(removeButton, 0);
                Grid.SetRow(removeButton, 0);

                removeButton.Click += RemoveButton_OnClick;

                grid.Children.Add(removeButton);

                Button duplicateButton = new Button
                {
                    Margin = new Thickness(5),
                    Content = "Duplicate",
                    Tag = objectIndex
                };
                Grid.SetColumn(duplicateButton, 1);
                Grid.SetRow(duplicateButton, 0);

                duplicateButton.Click += DuplicateButton_OnClick;

                grid.Children.Add(duplicateButton);

                Type objectType = objects[0].value.GetType();
                FieldInfo[] fields = objectType.GetFields();
                for (int j = 0; j < fields.Length; j++)
                {
                    string name = fields[j].Name;

                    Trace.WriteLine(name);
                    TextBlock label = new TextBlock
                    {
                        Text = name,
                        Margin = new Thickness(0, 0, 0, 5),
                        FontFamily = ConsolasFontFamily,
                        Padding = new Thickness(2, 4, 0, 2),
                    };
                    Grid.SetColumn(label, 0);
                    Grid.SetRow(label, j + 1);
                    grid.Children.Add(label);
                    if (j % 2 == 1)
                        label.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));

                    Type fieldType = fields[j].FieldType;

                    if (fieldType == typeof(bool))
                    {
                        CheckBox checkBox = new CheckBox
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 03, 0, 8),
                            ToolTip = "boolean",
                            //Padding = new Thickness(0, 3, 0, 3),
                            //Content = "Value",
                        };
                        Grid.SetColumn(checkBox, 1);
                        Grid.SetRow(checkBox, j + 1);
                        grid.Children.Add(checkBox);
                        label.ToolTip = "boolean";
                    }
                    else
                    {
                        TextBox textBox = new TextBox
                        {
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 5),
                            Padding = new Thickness(0, 3, 0, 3),
                            //Height = 26,
                            FontFamily = ConsolasFontFamily,
                        };
                        Grid.SetColumn(textBox, 1);
                        Grid.SetRow(textBox, j + 1);
                        grid.Children.Add(textBox);
                        if (fieldType == typeof(string))
                        {
                            string value = (string)fields[j].GetValue(currentObject);
                            textBox.Text = value != null ? $"\"{value}\"" : "null";
                            label.ToolTip = "String";
                            textBox.ToolTip = "String";
                        }
                        else if (fieldType == typeof(Vector3))
                        {
                            textBox.Text = ((Vector3)fields[j].GetValue(currentObject)).ToString();
                            textBox.KeyDown += Vector3_KeyDown;
                            label.ToolTip = "Vector3";
                            textBox.ToolTip = "Vector3";
                        }
                        else if (fieldType == typeof(int))
                        {
                            textBox.Text = ((int)fields[j].GetValue(currentObject)).ToString();
                            textBox.PreviewTextInput += Int_PreviewTextInput;
                            textBox.KeyDown += Int_KeyDown;
                            label.ToolTip = "32-bit integer";
                            textBox.ToolTip = "32-bit integer";
                        }
                        else if (fieldType == typeof(long))
                        {
                            textBox.Text = ((long)fields[j].GetValue(currentObject)).ToString();
                            textBox.PreviewTextInput += Int_PreviewTextInput;
                            textBox.KeyDown += Int_KeyDown;
                            label.ToolTip = "64-bit integer";
                            textBox.ToolTip = "64-bit integer";
                        }
                        else if (fieldType == typeof(float))
                        {
                            string text = ((float)fields[j].GetValue(currentObject)).ToString("0.0#################", Nfi) + 'f';
                            textBox.Text = text;
                            textBox.PreviewTextInput += Float_PreviewTextInput;
                            textBox.KeyDown += Float_KeyDown;
                            label.ToolTip = "float (32-bit decimal)";
                            textBox.ToolTip = "float (32-bit decimal)";
                        }
                        else if (fieldType == typeof(double))
                        {
                            string text = ((double)fields[j].GetValue(currentObject)).ToString("0.0#################", Nfi);
                            if (!text.Contains("."))
                                text += ".0";
                            textBox.Text = text;
                            textBox.PreviewTextInput += Double_PreviewTextInput;
                            textBox.KeyDown += Float_KeyDown;
                            label.ToolTip = "double (64-bit decimal)";
                            textBox.ToolTip = "double (64-bit decimal)";
                        }
                    }
                }

                expander.Content = grid;
                ObjectTabPanel.Children.Add(expander);
            }
        }

        private async void DuplicateButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button duplicateButton = (Button)sender;
            Grid grid = (Grid)duplicateButton.Parent;
            Expander originalExpander = (Expander)grid.Parent;

            Expander clone = null;
            await Dispatcher.InvokeAsync(() => clone = originalExpander.XamlClone());

            clone.IsExpanded = false;
            ObjectTabPanel.Children.Insert(ObjectTabPanel.Children.IndexOf(originalExpander), clone);

            FixExpanderNames();
        }

        private void FixExpanderNames()
        {
            for (int i = 1; i < ObjectTabPanel.Children.Count; i++)
            {
                Expander expander = (Expander)ObjectTabPanel.Children[i];
                expander.Header = $"{loadedDataType} {i - 1}";
            }
        }

        /// <summary>
        /// This field is for when it wants to add an expander, i.e. duplicate one, but there is no expander left
        /// </summary>
        private Expander duplicateExpander;

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button duplicateButton = (Button)sender;
            Grid grid = (Grid)duplicateButton.Parent;
            Expander expander = (Expander)grid.Parent;

            bool? result = MyMessageBox.Show(this, $"Are you sure you want to delete this {loadedDataType}?", "TOK ELF Editor", MessageBoxResult.Yes);
            if (result == true)
            {
                ObjectTabPanel.Children.Remove(expander);
                duplicateExpander = expander;
            }
        }

        private void Vector3_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            if (e.Key == Key.Enter)
            {
                Vector3? parsed = Vector3.FromString(textBox.Text);
                if (parsed != null)
                {
                    textBox.Text = parsed.ToString();
                }
                else
                    MessageBox.Show(window, "Invalid input", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private bool enterHandled;
        private void Int_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            if (e.Key == Key.Enter)
            {
                if (enterHandled == false)
                {
                    e.Handled = true;
                    if (IntRegex.IsMatch(((TextBox)e.Source).Text))
                    {
                        long.TryParse(textBox.Text, NumberStyles.Integer | NumberStyles.AllowExponent, new CultureInfo("en-US"), out long parsed);
                        textBox.Text = parsed.ToString();
                    }
                    else
                    {
                        enterHandled = true;
                        MessageBox.Show(window, "Invalid input", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    }
                }
                else
                    enterHandled = false;
            }
            else if (e.Key == Key.OemPeriod)
            {
                MessageBox.Show(window, "Field is an integer and doesn't support floating point numbers", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
            }
        }
        private void Float_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            if (e.Key == Key.Enter)
            {
                if (enterHandled == false)
                {
                    e.Handled = true;
                    if (StrictFloatRegex.IsMatch(((TextBox)e.Source).Text))
                    {
                        double.TryParse(textBox.Text, NumberStyles.Float, new CultureInfo("en-US"), out double parsed);
                        MessageBox.Show(parsed.ToString(Nfi));
                        textBox.Text = parsed.ToString("0.0#################", Nfi) + 'f';
                    }
                    else
                    {
                        enterHandled = true;
                        MessageBox.Show(window, "Invalid input", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    }
                }
                else
                    enterHandled = false;
            }
        }

        private static readonly Regex IntRegex = new Regex("^[0-9]+$");
        private static void Int_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IntRegex.IsMatch(e.Text);
        }
        private static readonly Regex FloatRegex = new Regex(@"^[0-9]*\.*[0-9]*$");
        private static readonly Regex StrictFloatRegex = new Regex(@"^[0-9]*\.*[0-9]*f?$");
        private static void Float_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            string text = textBox.Text;
            e.Handled = !FloatRegex.IsMatch(e.Text);
            if ((!text.EndsWith("f")) && text[text.Length - 2] != 'f')
            {
                ((TextBox)e.OriginalSource).AppendText("f");
            }
        }
        private static void Double_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !FloatRegex.IsMatch(e.Text);
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

        private void CommandBinding_Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            loadedDataType = GameDataType.NPC;

            OpenFileDialog dialog = new OpenFileDialog
            {
                FileName = "dispos_Npc.elf",
                DefaultExt = ".elf",
                Filter = "ELF Files (*.elf)|*.elf|Zstd Compressed ELF Files (*.elf.zst)|*.elf.zst"
            };
            bool? result = dialog.ShowDialog(window);
            if (result == true)
            {
                RemoveAllObjects();
                loadedBinary = ElfParser.ParseFile<NPC>(dialog.FileName, GameDataType.NPC);
                InitializeObjectsPanel(loadedBinary.Data.ToArray(), "NPC");
                fileSavePath = null;
                containingFolderPath = Path.GetDirectoryName(dialog.FileName) ?? @"C:\Users";
                AddRecentlyOpened(dialog.FileName);
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
                bool? result = dialog.ShowDialog(window);
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
                    File.WriteAllBytes(fileSavePath, fileSavePath.EndsWith(".zst") || fileSavePath.EndsWith(".zstd") ? compressor.Wrap(serialized) : serialized);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception);
                    MessageBox.Show(window, "Couldn't save the file. Maybe it's in use or doesn't exist", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    File.WriteAllBytes(fileSavePath, fileSavePath.EndsWith(".zst") || fileSavePath.EndsWith(".zstd") ? compressor.Wrap(serialized) : serialized);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception);
                    MessageBox.Show(window, "Couldn't save the file. Maybe it's in use or doesn't exist", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error);
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

                Expander expander = (Expander)objectPanel.Children[i];
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

                    if (j < 2)
                        continue;
                    else if (j % 2 == 0)
                    {
                        // label
                        propertyName = ((TextBlock)child).Text;
                        propertyType = typeof(NPC).GetField(propertyName).FieldType;
                    }
                    else
                    {
                        if (propertyType == typeof(bool))
                        {
                            CheckBox checkBox = (CheckBox)child;
                            propertyValue = checkBox.IsChecked;
                            Trace.WriteLine($"{propertyName}: {propertyType.Name} = {propertyValue} (from checkbox)");
                        }
                        else
                        {
                            // TextBox
                            TextBox textBox = (TextBox)child;
                            string text = textBox.Text;
                            switch (propertyType?.Name)
                            {
                                case "String":
                                    propertyValue = text.StartsWith("\"") && text.EndsWith("\"") ? text.Substring(1, text.Length - 2) : null;
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
                        }
                        typeof(NPC).GetField(propertyName).SetValue(currentNpc, propertyValue);
                    }

                }

                objects.Add(new Element<NPC>((NPC)currentNpc));
            }

            return objects;
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
            bool? result = openFileDialog.ShowDialog(window);
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
                bool? saveResult = saveFileDialog.ShowDialog(window);
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
            bool? openResult = openFileDialog.ShowDialog(window);
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
                bool? saveResult = saveFileDialog.ShowDialog(window);
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
            bool? result = MyMessageBox.Show(this, "Are you sure you want to remove all objects?", "TOK ELF Editor",
                MessageBoxResult.Yes);
            if (result == true)
            {
                duplicateExpander = (Expander)ObjectTabPanel.Children.Last();
                RemoveAllObjects();
            }
        }

        private async void Button_AddObject_OnClick(object sender, RoutedEventArgs e)
        {
            Expander clone = null;
            e.Handled = true;
            await Dispatcher.InvokeAsync(() => clone = (Expander)(ObjectTabPanel.Children.Count > 1 
                ? ObjectTabPanel.Children.Last() 
                : duplicateExpander).XamlClone());
            clone.IsExpanded = true;
            ObjectTabPanel.Children.Add(clone);
            FixExpanderNames();
        }
    }
}
