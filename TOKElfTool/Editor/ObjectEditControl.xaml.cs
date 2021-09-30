using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ElfLib;
using ElfLib.Types.Disposition;
using ElfLib.Types.Registry;
using Pointer = ElfLib.Pointer;

namespace TOKElfTool.Editor
{
    /// <summary>
    /// Interaction logic for ObjectEditControl.xaml
    /// </summary>
    public partial class ObjectEditControl : UserControl
    {
        public event RoutedEventHandler RemoveButtonClick;
        public event RoutedEventHandler DuplicateButtonClick;
        public event RoutedEventHandler ViewButtonClick;
        
        public event EventHandler<(ElfType type, int index)> HyperlinkClick;
        
        public event EventHandler ValueChanged;

        public bool ButtonPanelVisible
        {
            get => buttonPanelVisible;
            set
            {
                buttonPanelVisible = value;
                buttonPanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        public bool ViewButtonVisible
        {
            get => viewButtonVisible;
            set
            {
                viewButtonVisible = value;
                viewButton.Visibility = value == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        public bool ModifyButtonsEnabled
        {
            get => modifyButtonsEnabled;
            set
            {
                modifyButtonsEnabled = value;
                removeButton.IsEnabled = value;
                duplicateButton.IsEnabled = value;
            }
        }

        public int Index { get; set; }


        private ListEditControl innerControl;
        
        private readonly Brush secondaryBackground;

        private object currentObject;
        private List<Symbol> symbolTable;
        private readonly Dictionary<ElfType, List<Element<object>>> data;
        private readonly Dictionary<ElfType, List<long>> dataOffsets;
        private bool loaded;
        private bool viewButtonVisible;
        private bool buttonPanelVisible = true;
        private bool modifyButtonsEnabled;

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

        public FontFamily HeaderFont
        {
            get => ((TextBlock)Expander.Header).FontFamily;
            set => ((TextBlock)Expander.Header).FontFamily = value;
        } 
        
        public ObjectEditControl()
        {
            InitializeComponent();
        }
        
        public ObjectEditControl(object currentObject, string header, int index, List<Symbol> symbolTable, Dictionary<ElfType, List<Element<object>>> data, Dictionary<ElfType, List<long>> dataOffsets)
        {
            InitializeComponent();

            this.Index = index;
            Expander.Header = new TextBlock { Text = header };

            this.currentObject = currentObject;
            this.symbolTable = symbolTable;
            this.data = data;
            this.dataOffsets = dataOffsets;

            string backgroundName = currentObject switch
            {
                NpcModelFiles _ => ("files"),
                NpcModelState _ => ("state"),
                NpcModelSubState _ => ("subState"),
                NpcModelFace _ => ("face"),
                NpcModelAnime _ => ("anime"),
                _ => null,
            };
            
            Background = backgroundName != null ? (Brush)FindResource(backgroundName) : null;

            if (Background != null)
            {
                Expander.Padding = new Thickness(4);

                secondaryBackground = (Brush)FindResource(backgroundName + "Secondary");
            }
            else
            {
                secondaryBackground = (Brush)FindResource("secondary");
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


        public void Generate()
        {
            if (!loaded)
            {
                Pointer? arrayPointer = null;
                ElfType arrayLocation = 0;
                string expanderTitle = null;
                int arrayLength;
                
                // fields
                Type objectType = currentObject.GetType();
                FieldInfo[] fields = objectType.GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    PointerAttribute pointerAttribute = fields[i].GetCustomAttribute<PointerAttribute>();
                    PointerArrayLengthAttribute arrayLengthAttribute = fields[i].GetCustomAttribute<PointerArrayLengthAttribute>();

                    if ((pointerAttribute == null || (Pointer)fields[i].GetValue(currentObject) == Pointer.NULL) && arrayLengthAttribute == null)
                    {
                        Grid.RowDefinitions.Add(new RowDefinition());
                        AddFieldControls(currentObject, fields[i], Grid, i, symbolTable);
                    }
                    else if (pointerAttribute != null)
                    {
                        arrayPointer = (Pointer)fields[i].GetValue(currentObject);
                        expanderTitle = fields[i].Name;
                        arrayLocation = pointerAttribute.Location;
                    }
                    else if (arrayLengthAttribute != null)
                    {
                        arrayLength = (int)fields[i].GetValue(currentObject);
                    }
                }

                if (arrayPointer != null)
                {
                    List<object> objects = (List<object>)data[arrayLocation][dataOffsets[arrayLocation].IndexOf(((Pointer)arrayPointer).AsLong)].value;
                    
                    innerControl = new ListEditControl
                    {
                        Header = expanderTitle,
                        Objects = objects,
                        ChildHeader = objects.Count > 0 ? objects[0].GetType().Name : "",
                        Data = data,
                        DataOffsets = dataOffsets,
                        HeaderFont = ConsolasFontFamily,
                        Margin = new Thickness(6, 0, 0, 0),
                        Index = 0,
                    };

                    DockPanel.SetDock(innerControl, Dock.Bottom);
                    
                    dockPanel.Children.Insert(1, innerControl);
                }
                
                if (currentObject is MaplinkHeader)
                {
                    duplicateButton.IsEnabled = false;
                    removeButton.IsEnabled = false;
                }

                loaded = true;
            }
        }

        public void ApplyChangesToObject()
        {
            for (int i = 0; i < Grid.Children.Count; i += 2)
            {
                TextBlock textBlock = (TextBlock)Grid.Children[i];
                UIElement valueControl = Grid.Children[i + 1];
                
                string fieldName = textBlock.Text;
                FieldInfo currentField = currentObject.GetType().GetField(fieldName);
                Type fieldType = currentField.FieldType;
                
                object propertyValue = ReadFromControl(valueControl, fieldType);
                
                currentField.SetValue(currentObject, propertyValue);
            }

            innerControl?.ApplyChangesToObject();
        }
        
        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        private static object ReadFromControl(UIElement child, Type propertyType)
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
            
            return propertyType.Name switch
            {
                nameof(String) => text.StartsWith("\"") && text.EndsWith("\"")
                    ? text.Substring(1, text.Length - 2)
                    : null,
                nameof(Vector3) => Vector3.FromString(text),
                nameof(Byte) => byte.Parse(text),
                nameof(Int32) => int.Parse(text),
                nameof(Int64) => long.Parse(text),
                nameof(Single) => float.Parse(text.EndsWith("f") ? text.Substring(0, text.Length - 1) : text),
                nameof(Double) => double.Parse(text),
                
                _ => throw new Exception("Couldn't read the property value"),
            };

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
                label.Background = secondaryBackground;

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
                checkBox.Click += (sender, args) => ValueChanged?.Invoke(this, args);

                return;
            }

            // ComboBox
            if (fieldType.BaseType == typeof(Enum))
            {
                ComboBox comboBox = new ComboBox
                {
                    Margin = new Thickness(0, 0, 0, 5),
                };
                if (StringEnumAttribute.IsStringEnum(fieldType))
                {
                    comboBox.IsEditable = true;
                    comboBox.Style = (Style)Application.Current.Resources["StringEnumComboBox"];
                    comboBox.Height = 22;
                    comboBox.KeyDown += (sender, e) =>
                    {
                        if (e.Key == Key.Enter)
                        {
                            // if it contains the ingame value, get the enum value and then the display name
                            object fromIdentifier = StringEnumAttribute.GetEnumValueFromString(comboBox.Text, fieldType);
                            if (fromIdentifier != null)
                            {
                                string evaluated = StringEnumAttribute.GetDisplayName(fromIdentifier, fieldType);
                                comboBox.Text = evaluated;
                            }

                            ValueChanged?.Invoke(this, e);
                        }
                    };
                    comboBox.SelectionChanged += (sender, e) =>
                    {
                        ValueChanged?.Invoke(this, e);
                    };
                }

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

            if (field.Name == "model_files_ptr")
            {
                textBox.IsReadOnly = true;
                textBox.TextDecorations = (TextDecorationCollection)FindResource("Underline");
                textBox.Foreground = (SolidColorBrush)FindResource("LinkColor");

                int targetIndex = dataOffsets[ElfType.Files].IndexOf(((Pointer)field.GetValue(currentObject)).AsLong);
                textBox.Text = $"Files Object {targetIndex} in Files Data";

                textBox.ToolTip = "Click to Go to Target";
                textBox.Cursor = Cursors.Hand;
                
                bool isTextBoxClicked = false;
                textBox.PreviewMouseDown += (sender, e) =>
                    isTextBoxClicked = true;
                textBox.PreviewMouseUp += (sender, e) =>
                {
                    if (isTextBoxClicked)
                        HyperlinkClick?.Invoke(this, (ElfType.Files, targetIndex));
                };
                Window.GetWindow(this)!.MouseUp += (sender, e) =>
                    isTextBoxClicked = false;
                
                return;
            }
            
            if (field.Name == "state_ptr")
            {
                textBox.IsReadOnly = true;
                textBox.TextDecorations = (TextDecorationCollection)FindResource("Underline");
                textBox.Foreground = (SolidColorBrush)FindResource("LinkColor");

                int targetIndex = dataOffsets[ElfType.State].IndexOf(((Pointer)field.GetValue(currentObject)).AsLong);
                textBox.Text = $"State Object {targetIndex} in State Data";

                textBox.ToolTip = "Click to Go to Target";
                textBox.Cursor = Cursors.Hand;
                
                bool isTextBoxClicked = false;
                textBox.PreviewMouseDown += (sender, e) =>
                    isTextBoxClicked = true;
                textBox.PreviewMouseUp += (sender, e) =>
                {
                    if (isTextBoxClicked)
                        HyperlinkClick?.Invoke(this, (ElfType.State, targetIndex));
                };
                Window.GetWindow(this)!.MouseUp += (sender, e) =>
                    isTextBoxClicked = false;
                
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
                case "Int16":
                    textBox.Text = ((short)field.GetValue(currentObject)).ToString();
                    textBox.PreviewTextInput += Int_PreviewTextInput;
                    textBox.KeyDown += Int_KeyDown;
                    label.ToolTip = "16-bit integer";
                    textBox.ToolTip = "16-bit integer";
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
                case "Pointer":
                    textBox.Text = ((Pointer)field.GetValue(currentObject)).ToString();
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
                    throw new Exception("Type not supported");
            }


        }

        
        public ObjectEditControl Clone()
        {
            ObjectEditControl clone = new ObjectEditControl()
            {
                loaded = true,
                RemoveButtonClick = RemoveButtonClick,
                DuplicateButtonClick = DuplicateButtonClick,
                ValueChanged = ValueChanged,
                ButtonPanelVisible = ButtonPanelVisible,
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

        private void Expander_OnExpanded(object sender, RoutedEventArgs e)
        {
            Generate();
            e.Handled = true;
        }
        
        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveButtonClick?.Invoke(this, e);
        }

        private void DuplicateButton_OnClick(object sender, RoutedEventArgs e)
        {
            DuplicateButtonClick?.Invoke(this, e);
        }
        
        private void ViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ViewButtonVisible)
            {
                ViewButtonClick?.Invoke(this, e);
            }
        }
    }
}
