using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for ObjectEditControl.xaml
    /// </summary>
    public partial class ObjectEditControl : UserControl
    {
        public event RoutedEventHandler RemoveButtonClick;
        public event RoutedEventHandler DuplicateButtonClick;

        public event EventHandler ValueChanged;


        private static readonly FontFamily ConsolasFontFamily = new FontFamily("Consolas");
        private static readonly NumberFormatInfo Nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = ".",
        };

        public object Header
        {
            get => Expander.Header;
            set => Expander.Header = value;
        }
        public bool IsExpanded
        {
            get => Expander.IsExpanded;
            set => Expander.IsExpanded = value;
        }

        public ObjectEditControl()
        {
            InitializeComponent();
        }

        public ObjectEditControl(object currentObject, string header, int index, List<Symbol> symbolTable)
        {
            InitializeComponent();

            this.Index = index;
            Expander.Header = header;

            // fields
            Type objectType = currentObject.GetType();
            FieldInfo[] fields = objectType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                Grid.RowDefinitions.Add(new RowDefinition());
                AddFieldControls(currentObject, fields[i], Grid, i, symbolTable);
            }

            if (currentObject is MaplinkHeader)
            {
                duplicateButton.IsEnabled = false;
                removeButton.IsEnabled = false;
            }
        }

        public ObjectEditControl Clone()
        {
            ObjectEditControl clone = new ObjectEditControl
            {
                RemoveButtonClick = RemoveButtonClick,
                DuplicateButtonClick = DuplicateButtonClick,
                ValueChanged = ValueChanged,
            };
            for (int i = 0; i < Grid.Children.Count; i++)
            {
                UIElement element = Grid.Children[i].XamlClone();
                clone.Grid.Children.Add(element);
                clone.Grid.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(element, i / 2);
            }
            return clone;
        }


        private void AddFieldControls(object currentObject, FieldInfo field, Grid grid, int fieldIndex, List<Symbol> symbolTable)
        {
            string name = field.Name;

            TextBlock label = new TextBlock
            {
                Text = name,
                Margin = new Thickness(0, 0, 0, 5),
                FontFamily = ConsolasFontFamily,
                Padding = new Thickness(2, 4, 0, 2),
            };
            Grid.SetColumn(label, 0);
            Grid.SetRow(label, fieldIndex);
            grid.Children.Add(label);
            if (fieldIndex % 2 == 1)
                label.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));

            Type fieldType = field.FieldType;

            // checkbox
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
                Grid.SetRow(checkBox, fieldIndex);
                grid.Children.Add(checkBox);
                label.ToolTip = "boolean";
                checkBox.Click += (sender, args) => ValueChanged?.Invoke(sender, args);

                return;
            }

            // ComboBox
            if (fieldType.BaseType == typeof(Enum))
            {
                ComboBox comboBox = new ComboBox
                {
                    Margin = new Thickness(0, 0, 0, 5),
                };
                IEnumerable<FieldInfo> enumFields = fieldType.GetFields()
                    .Where(value => value.IsStatic);

                foreach (FieldInfo enumField in enumFields)
                {
                    EnumMetadataAttribute[] attributes = enumField.GetCustomAttributes(typeof(EnumMetadataAttribute), false).Cast<EnumMetadataAttribute>().ToArray();
                    comboBox.Items.Add(attributes.Length > 0 ? attributes[0].DisplayName : enumField.Name);
                }

                Trace.WriteLine(Array.IndexOf(Enum.GetValues(fieldType), field.GetValue(currentObject)));
                comboBox.SelectedIndex = Array.IndexOf(Enum.GetValues(fieldType), field.GetValue(currentObject));

                Grid.SetColumn(comboBox, 1);
                Grid.SetRow(comboBox, fieldIndex);

                grid.Children.Add(comboBox);
                label.ToolTip = fieldType.Name;

                comboBox.DropDownClosed += (sender, args) => ValueChanged?.Invoke(sender, args);

                return;
            }


            // TextBox
            TextBox textBox = new TextBox
            {
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0, 3, 0, 3),
                //Height = 26,
                FontFamily = ConsolasFontFamily,
            };
            Grid.SetColumn(textBox, 1);
            Grid.SetRow(textBox, fieldIndex);
            grid.Children.Add(textBox);

            if (field.Name == "nodes_start_ptr")
            {
                textBox.Text = symbolTable[8].Name;
                textBox.IsEnabled = false;

                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                textBox.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                textBox.Foreground = new SolidColorBrush(Color.FromRgb(109, 109, 109));
                label.Margin = new Thickness(label.Margin.Left, label.Margin.Top, 4, label.Margin.Bottom);
                return;
            }

            textBox.KeyDown += (sender, args) => ValueChanged?.Invoke(sender, args);
            Trace.WriteLine(fieldType.Name);
            switch (fieldType.Name)
            {
                case "String":
                    string value = (string)field.GetValue(currentObject);
                    textBox.Text = value != null ? $"\"{value}\"" : "null";
                    label.ToolTip = "String";
                    textBox.ToolTip = "String";
                    break;
                case "Vector3":
                    textBox.Text = ((Vector3)field.GetValue(currentObject)).ToString();
                    textBox.KeyDown += Vector3_KeyDown;
                    label.ToolTip = "Vector3";
                    textBox.ToolTip = "Vector3";
                    break;
                case "Byte":
                    textBox.Text = ((byte)field.GetValue(currentObject)).ToString();
                    textBox.PreviewTextInput += Int_PreviewTextInput;
                    textBox.KeyDown += Int_KeyDown;
                    label.ToolTip = "byte (8-bit integer or boolean)";
                    textBox.ToolTip = "byte (8-bit integer or boolean)";
                    break;
                case "Int32":
                    textBox.Text = ((int)field.GetValue(currentObject)).ToString();
                    textBox.PreviewTextInput += Int_PreviewTextInput;
                    textBox.KeyDown += Int_KeyDown;
                    label.ToolTip = "32-bit integer";
                    textBox.ToolTip = "32-bit integer";
                    break;
                case "Int64":
                    textBox.Text = ((long)field.GetValue(currentObject)).ToString();
                    textBox.PreviewTextInput += Int_PreviewTextInput;
                    textBox.KeyDown += Int_KeyDown;
                    label.ToolTip = "64-bit integer";
                    textBox.ToolTip = "64-bit integer";
                    break;
                case "Single":
                    string floatText = ((float)field.GetValue(currentObject)).ToString("0.0#################", Nfi) + 'f';
                    textBox.Text = floatText;
                    textBox.PreviewTextInput += Float_PreviewTextInput;
                    textBox.KeyDown += Float_KeyDown;
                    label.ToolTip = "float (32-bit decimal)";
                    textBox.ToolTip = "float (32-bit decimal)";
                    break;
                case "Double":
                    string doubleText = ((double)field.GetValue(currentObject)).ToString("0.0#################", Nfi);
                    textBox.Text = doubleText;
                    textBox.PreviewTextInput += Double_PreviewTextInput;
                    textBox.KeyDown += Float_KeyDown;
                    label.ToolTip = "double (64-bit decimal)";
                    textBox.ToolTip = "double (64-bit decimal)";
                    break;
                case "ElfStringPointer":
                    throw new Exception("ElfStringPointer didn't get replaced with string");
                default:
                    throw new NotImplementedException();
            }


        }


        #region Input field callbacks

        private bool enterHandled;
        private static void Vector3_KeyDown(object sender, KeyEventArgs e)
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
                    MessageBox.Show("Invalid input", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }
        private void Int_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)e.OriginalSource;
            switch (e.Key)
            {
                case Key.Enter when enterHandled == false:
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
                            MessageBox.Show("Invalid input", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                        }

                        break;
                    }
                case Key.Enter:
                    enterHandled = false;
                    break;
                case Key.OemPeriod:
                    MessageBox.Show("Field is an integer and doesn't support floating point numbers", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    break;
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
                        MessageBox.Show("Invalid input", "TOK ELF Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    }
                }
                else
                    enterHandled = false;
            }
        }

        private static readonly Regex IntRegex = new Regex("^[0-9]+$");
        private static readonly Regex FloatRegex = new Regex(@"^[0-9]*\.*[0-9]*$");
        private static readonly Regex StrictFloatRegex = new Regex(@"^[0-9]*\.*[0-9]*f?$");

        public int Index { get; set; }

        private static void Int_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IntRegex.IsMatch(e.Text);
        }
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

        #endregion

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveButtonClick.Invoke(sender, e);
        }

        private void DuplicateButton_OnClick(object sender, RoutedEventArgs e)
        {
            DuplicateButtonClick.Invoke(sender, e);
        }
    }
}
