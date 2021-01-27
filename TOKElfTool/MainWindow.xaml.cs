using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Microsoft.Win32;
using ElfLib;
using System.Text.RegularExpressions;
using System.Globalization;
using stStackPanel = AutoGrid.StackPanel;
using stAutoGrid = AutoGrid.AutoGrid;
using System.Diagnostics;
using System.IO;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //Trace.Listeners.Add()

            InitializeComponent();

            emptyLabel.Visibility = Visibility.Visible;
            scrollViewer.Visibility = Visibility.Collapsed;

            Trace.WriteLine("ajklsdflkasdjlksdsdlkjlkfdssdsdfsd");

            //InitializeObjectsPanel(new object[] { null, null, null, null }, "NPC");
        }

        private static FontFamily consolasFontFamily = new FontFamily("Consolas");

        private static NumberFormatInfo nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = "."
        };

        private const GameDataType loadedDataType = GameDataType.NPC;

        private void InitializeObjectsPanel<T>(Element<T>[] objects, string objectName)
        {
            emptyLabel.Visibility = Visibility.Collapsed;
            scrollViewer.Visibility = Visibility.Visible;

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

                grid.Children.Add(removeButton);

                Button duplicateButton = new Button
                {
                    Margin = new Thickness(5),
                    Content = "Duplicate",
                    Tag = objectIndex
                };
                Grid.SetColumn(duplicateButton, 1);
                Grid.SetRow(duplicateButton, 0);

                grid.Children.Add(duplicateButton);

                Type objectType = objects[0].value.GetType();
                FieldInfo[] fields = objectType.GetFields();
                for (int j = 0; j < fields.Length; j++)
                {
                    string name = fields[j].Name;

                    Label label = new Label
                    {
                        Content = name,
                        Margin = new Thickness(0, 0, 0, 5),
                        FontFamily = consolasFontFamily,
                    };
                    Grid.SetColumn(label, 0);
                    Grid.SetRow(label, j + 1);
                    grid.Children.Add(label);

                    Type fieldType = fields[j].FieldType;

                    TextBox textBox = new TextBox
                    {
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 5),
                        Height = 26,
                        FontFamily = consolasFontFamily,
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
                        string text = ((float)fields[j].GetValue(currentObject)).ToString("0.0", nfi) + 'f';
                        textBox.Text = text;
                        textBox.PreviewTextInput += Float_PreviewTextInput;
                        textBox.KeyDown += Float_KeyDown;
                        label.ToolTip = "float (32-bit decimal)";
                        textBox.ToolTip = "float (32-bit decimal)";
                    }
                    else if (fieldType == typeof(double))
                    {
                        string text = ((float)fields[j].GetValue(currentObject)).ToString();
                        if (!text.Contains("."))
                            text += ".0";
                        textBox.Text = ((double)fields[j].GetValue(currentObject)).ToString();
                        textBox.PreviewTextInput += Double_PreviewTextInput;
                        textBox.KeyDown += Float_KeyDown;
                        label.ToolTip = "double (64-bit decimal)";
                        textBox.ToolTip = "double (64-bit decimal)";
                    }

                }

                expander.Content = grid;
                objectTabPanel.Children.Add(expander);
            }
        }

        private void Vector3_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            if (e.Key == Key.Enter)
            {
                Vector3? parsed = Vector3.FromString(textBox.Text);
                if(parsed != null)
                {
                    textBox.Text = parsed.ToString();
                }
                else
                    MessageBox.Show(window, "Invalid input", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private bool enterHandled = false;
        private void Int_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            if (e.Key == Key.Enter)
            {
                if (enterHandled == false)
                {
                    e.Handled = true;
                    if (intRegex.IsMatch(((TextBox)e.Source).Text))
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
                    if (strictFloatRegex.IsMatch(((TextBox)e.Source).Text))
                    {
                        double.TryParse(textBox.Text, NumberStyles.Float, new CultureInfo("en-US"), out double parsed);
                        MessageBox.Show(parsed.ToString(nfi));
                        textBox.Text = parsed.ToString("0.0", nfi) + 'f';
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

        private static readonly Regex intRegex = new Regex("^[0-9]+$");
        private static void Int_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !intRegex.IsMatch(e.Text);
        }
        private static readonly Regex floatRegex = new Regex(@"^[0-9]*\.*[0-9]*$");
        private static readonly Regex strictFloatRegex = new Regex(@"^[0-9]*\.*[0-9]*f?$");
        private static void Float_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            string text = textBox.Text;
            e.Handled = !floatRegex.IsMatch(e.Text);
            if ((!text.EndsWith("f")) && text[text.Length - 2] != 'f')
            {
                ((TextBox)e.OriginalSource).AppendText("f");
            }
        }
        private static void Double_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !floatRegex.IsMatch(e.Text);
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

        private ElfBinary<NPC> loadedBinary;

        private void CommandBinding_Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                FileName = "dispos_Npc.elf",
                DefaultExt = ".elf",
                Filter = "ELF Files (*.elf)|*.elf|Zstd Compressed ELF Files (*.elf.zst)|*.elf.zst"
            };
            bool? result = dialog.ShowDialog(window);
            if (result == true)
            {
                loadedBinary = ElfParser.ParseFile<NPC>(dialog.FileName, GameDataType.NPC);
                InitializeObjectsPanel(loadedBinary.Data.ToArray(), "NPC");
            }
        }

        private void CommandBinding_Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private string savePath = null;

        private void CommandBinding_Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<Element<NPC>> objects = CollectObjects(objectTabPanel);

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
            if (savePath != null)
                File.WriteAllBytes(savePath, serialized);
        }

        private List<Element<NPC>> CollectObjects(StackPanel objectTabPanel)
        {
            List<Element<NPC>> objects = new List<Element<NPC>>();

            // go through all npc's
            for (int i = 0; i < objectTabPanel.Children.Count; i++)
            {
                if (i == 0)
                    continue;
                
                Expander expander = (Expander)objectTabPanel.Children[i];
                Grid grid = (Grid)expander.Content;

                Trace.WriteLine(expander);

                // go through all property controls
                object currentNpc = new NPC();
                string propertyName = "";
                Type propertyType = null;
                object propertyValue = null;

                for (int j = 0; j < grid.Children.Count; j++)
                {
                    var child = grid.Children[j];

                    if (j < 2)
                        continue;
                    else if (j % 2 == 0)
                    {
                        // label
                        propertyName = (string)((Label)child).Content;
                        propertyType = typeof(NPC).GetField(propertyName).FieldType;
                    }
                    else
                    {
                        // textbox
                        TextBox textBox = (TextBox)child;
                        string text = textBox.Text;
                        switch(propertyType.Name)
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
                        Trace.WriteLine($"{propertyName}: {propertyType.Name} = {propertyValue} (original {text})");
                        typeof(NPC).GetField(propertyName).SetValue(currentNpc, propertyValue);
                    }

                }

                objects.Add(new Element<NPC>((NPC)currentNpc));
            }

            return objects;
        }
    }
}
