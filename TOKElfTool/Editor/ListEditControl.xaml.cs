using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ElfLib;

namespace TOKElfTool.Editor
{
    /// <summary>
    /// Interaction logic for ObjectEditControl.xaml
    /// </summary>
    public partial class ListEditControl : UserControl
    {
        public event RoutedEventHandler RemoveButtonClick;
        public event RoutedEventHandler DuplicateButtonClick;
        public event RoutedEventHandler ViewButtonClick;
        
        public event EventHandler<(ElfType type, int index)> HyperlinkClick;
        
        public event EventHandler<int> ValueChanged;

        private static readonly FontFamily ConsolasFontFamily = new FontFamily("Consolas");
        private static readonly NumberFormatInfo Nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = ".",
        };
        
        public List<object> Objects { get; set; }
        
        public string ChildHeader { get; set; }
        
        public Dictionary<ElfType, List<Element<object>>> Data { get; set; }
        
        public Dictionary<ElfType, List<long>> DataOffsets { get; set; }

        public string Header { get => headerText.Text; set => headerText.Text = value; }
        
        public FontFamily HeaderFont { get => headerText.FontFamily; set => headerText.FontFamily = value; }
        
        public bool IsExpanded { get => Expander.IsExpanded; set => Expander.IsExpanded = value; }


        private bool loaded;
        
        
        public ListEditControl()
        {
            InitializeComponent();
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

        private void Expander_OnExpanded(object sender, RoutedEventArgs e)
        {
            Generate();
            e.Handled = true;
        }

        public void Generate()
        {
            if (!loaded)
            {
                stackPanel.Children.Clear();

                for (int i = 0; i < Objects.Count; i++)
                {
                    ObjectEditControl control = new ObjectEditControl(Objects[i], $"{(ChildHeader)} {i}", i, null, Data, DataOffsets);
                    stackPanel.Children.Add(control);
                }
            }
        }
        
        public void ApplyChangesToObject()
        {
            foreach (ObjectEditControl control in stackPanel.Children)
            {
                control.ApplyChangesToObject();
            }
        }

        private void ViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            // if (ViewButtonVisible)
            // {
            //     ViewButtonClick?.Invoke(this, e);
            // }
        }
    }
}
