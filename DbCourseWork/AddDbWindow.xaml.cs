using System.Windows;

namespace DbCourseWork
{
    public partial class AddDbWindow
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
