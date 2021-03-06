﻿using System;
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

namespace TOKElfTool.Search
{
    /// <summary>
    /// Interaction logic for SearchBar.xaml
    /// </summary>
    public partial class SearchBar : UserControl
    {
        public event EventHandler<string> OnSearch;

        public event EventHandler StartIndexing;

        public bool HasIndexed { get; set; }

        public string Text
        {
            get => textBox.Text;
            set => textBox.Text = value;
        }

        public SearchBar()
        {
            InitializeComponent();
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            placeholder.Visibility = textBox.Text != "" ? Visibility.Hidden : Visibility.Visible;

            if (!HasIndexed)
            {
                StartIndexing?.Invoke(this, EventArgs.Empty);
                HasIndexed = true;
            }
        }

        private void TextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnSearch?.Invoke(this, textBox.Text);
            }
        }
    }
}
