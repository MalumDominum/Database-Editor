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
using System.Windows.Shapes;

namespace DbCourseWork
{
    public partial class AddDbWindow : Window
    {
        public string Server => ServerBox.Text;
        public string Port => PortBox.Text;
        public string Database => DatabaseBox.Text;
        public string User => UserBox.Text;
        public string Password => PasswordBox.Password;
        public AddDbWindow()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
