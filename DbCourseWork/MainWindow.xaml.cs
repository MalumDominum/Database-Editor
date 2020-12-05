using System;
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

namespace DbCourseWork
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ArrowPressed_OnKeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            // Обновление таблиц
        }

        private void TreeViewItem_OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Вывод таблицы на экран
        }

        private void AddDb_Click(object sender, RoutedEventArgs e)
        {
            var addDbWindow = new AddDbWindow();

            if (addDbWindow.ShowDialog() == true)
                MessageBox.Show(addDbWindow.Password == "12345678" ? "Авторизация пройдена" : "Неверный пароль");
            else
                MessageBox.Show("Авторизация не пройдена");
        }

        private void EditMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
