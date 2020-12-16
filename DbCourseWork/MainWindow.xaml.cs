using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
using Npgsql;

namespace DbCourseWork
{
    public partial class MainWindow : Window
    {
        enum SqlServer { MsSql, PostgreSql }
        public static DataGrid CustomGrid { get; private set; }
        private static Dictionary<string, PostgreCommunication> PostgreCommunications { get; set; } = new Dictionary<string, PostgreCommunication>();
        private static Dictionary<string, MsCommunication> MsCommunications { get; set; } = new Dictionary<string, MsCommunication>();
        private static SqlServer CurrentServer { get; set; }
        private static DataTable CurrentTable { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            CustomGrid = DataGrid;

            var connectionStrings = ConfigurationManager.ConnectionStrings;
            foreach (ConnectionStringSettings connectionString in connectionStrings)
            {
                switch (connectionString.ProviderName)
                {
                    case "PostgreSQL":
                        PostgreCommunications.Add(connectionString.Name, new PostgreCommunication(connectionString));
                        PostgreCommunications[connectionString.Name].FillDs();
                        FillTreeView(DatabasesTreeView, PostgreCommunications[connectionString.Name]);
                        break;
                    case "MS SQL":
                        MsCommunications.Add(connectionString.Name, new MsCommunication(connectionString));
                        MsCommunications[connectionString.Name].FillDs();
                        FillTreeView(DatabasesTreeView, MsCommunications[connectionString.Name]);
                        break;
                }
            }
        }

        private void SaveChangesButton_OnClick(object sender, RoutedEventArgs e)
        {
            PostgreCommunications[CurrentTable.TableName].UpdateDb(CurrentTable).GetAwaiter();
        }

        private void ReturnChangesButton_OnClick(object sender, RoutedEventArgs e)
        {
            CustomGrid.DataContext = CurrentTable;
        }

        private void ArrowPressed_OnKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void AddDb_Click(object sender, RoutedEventArgs e)
        {
            var addDbWindow = new AddDbWindow();

            if (addDbWindow.ShowDialog() == false) return;

            var communication = 
                new PostgreCommunication(addDbWindow.Database, addDbWindow.User,
                    addDbWindow.Server, addDbWindow.Port,addDbWindow.Password);

            PostgreCommunications.Add(addDbWindow.Database, communication);
        }

        private void EditMenuItem_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void FillTreeView(TreeView treeView, MsCommunication communication)
        {
            var dbContextMenu = new ContextMenu();
            var dbRefreshItem = new MenuItem { Header = "Refresh" };
            dbRefreshItem.Click += DbMenuItem_Refresh;
            dbContextMenu.Items.Add(dbRefreshItem);

            var dbDisconnectItem = new MenuItem { Header = "Disconnect" };
            dbDisconnectItem.Click += DbMenuItem_Disconnect;
            dbContextMenu.Items.Add(dbDisconnectItem);


            var tableContextMenu = new ContextMenu();
            var tableViewAllItem = new MenuItem { Header = "View all rows" };
            tableViewAllItem.Click += ViewAll_Click;
            tableContextMenu.Items.Add(tableViewAllItem);

            var tableViewFirstItem = new MenuItem { Header = "View first 100 rows" };
            tableViewAllItem.Click += ViewFirst_Click;
            tableContextMenu.Items.Add(tableViewFirstItem);

            var tableViewLastItem = new MenuItem
            {
                Header = "View last 100 rows",
                CommandParameter =
                    "{ Binding PlacementTarget," +
                      "RelativeSource=" +
                      "{" +
                         "RelativeSource FindAncestor," +
                         "AncestorType = { x:Type ContextMenu }" +
                    "}}"
            };
            tableViewAllItem.Click += ViewLast_Click;
            tableContextMenu.Items.Add(tableViewLastItem);

            var tableViewFilteredItem = new MenuItem { Header = "View filter options" };
            tableViewAllItem.Click += ViewFiltered_Click;
            tableContextMenu.Items.Add(tableViewFilteredItem);

            /*var columnContextMenu = new ContextMenu();
            var columnMenuItem = new MenuItem { Header = "" };
            //attributeMenuItem.Click += AttributeMenuItem_Click;
            columnContextMenu.Items.Add(columnMenuItem);*/


            var dbView = new TreeViewItem
            {
                Header = communication.Ds.DataSetName,
                ContextMenu = dbContextMenu
            };
            treeView.Items.Add(dbView);

            foreach (DataTable table in communication.Ds.Tables)
            {
                var tableView = new TreeViewItem
                {
                    Header = table.TableName,
                    ContextMenu = tableContextMenu
                };
                dbView.Items.Add(tableView);

                foreach (DataColumn column in table.Columns)
                {
                    var columnView = new TreeViewItem { Header = column.ColumnName };
                    tableView.Items.Add(columnView);
                }
            }
        }

        private void FillTreeView(TreeView treeView, PostgreCommunication communication)
        {
            var dbContextMenu = new ContextMenu();
            var dbRefreshItem = new MenuItem { Header = "Refresh" };
            dbRefreshItem.Click += DbMenuItem_Refresh;
            dbContextMenu.Items.Add(dbRefreshItem);

            var dbDisconnectItem = new MenuItem { Header = "Disconnect" };
            dbDisconnectItem.Click += DbMenuItem_Disconnect;
            dbContextMenu.Items.Add(dbDisconnectItem);


            var tableContextMenu = new ContextMenu();
            var tableViewAllItem = new MenuItem { Header = "View all rows" };
            tableViewAllItem.Click += ViewAll_Click;
            tableContextMenu.Items.Add(tableViewAllItem);

            var tableViewFirstItem = new MenuItem { Header = "View first 100 rows" };
            tableViewAllItem.Click += ViewFirst_Click;
            tableContextMenu.Items.Add(tableViewFirstItem);

            var tableViewLastItem = new MenuItem
            {
                Header = "View last 100 rows",
                CommandParameter =
                    "{ Binding PlacementTarget," +
                      "RelativeSource=" +
                      "{" +
                         "RelativeSource FindAncestor," +
                         "AncestorType = { x:Type ContextMenu }" +
                    "}}"
            };
            tableViewAllItem.Click += ViewLast_Click;
            tableContextMenu.Items.Add(tableViewLastItem);

            var tableViewFilteredItem = new MenuItem { Header = "View filter options" };
            tableViewAllItem.Click += ViewFiltered_Click;
            tableContextMenu.Items.Add(tableViewFilteredItem);

            /*var columnContextMenu = new ContextMenu();
            var columnMenuItem = new MenuItem { Header = "" };
            //attributeMenuItem.Click += AttributeMenuItem_Click;
            columnContextMenu.Items.Add(columnMenuItem);*/


            var dbView = new TreeViewItem
            {
                Header = communication.Ds.DataSetName,
                ContextMenu = dbContextMenu
            };
            treeView.Items.Add(dbView);

            foreach (DataTable table in communication.Ds.Tables)
            {
                var tableView = new TreeViewItem
                {
                    Header = table.TableName,
                    ContextMenu = tableContextMenu
                };
                dbView.Items.Add(tableView);

                foreach (DataColumn column in table.Columns)
                {
                    var columnView = new TreeViewItem { Header = column.ColumnName };
                    tableView.Items.Add(columnView);
                }
            }
        }

        private void DbMenuItem_Refresh(object sender, RoutedEventArgs e)
        {
            
        }

        private void DbMenuItem_Disconnect(object sender, RoutedEventArgs e)
        {
            // Сделать серым в TreeView или удалить
        }

        private void ViewAll_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem)) return;
            MessageBox.Show("1");
            if (!(menuItem.Parent is ContextMenu contextMenu)) return;
            MessageBox.Show("2");
            if (!(contextMenu.PlacementTarget is TreeViewItem viewItem)) return;
            MessageBox.Show("3");
            var parentControl = GetTreeViewItemParent(viewItem);
            var parentView = parentControl as TreeViewItem;

            var dbHeader = parentView.Header.ToString();
            CurrentTable = PostgreCommunications[dbHeader].Ds.Tables[viewItem.Header.ToString()];
            PostgreCommunications[dbHeader].ShowOnDataGrid(CurrentTable).GetAwaiter();
        }

        private void ViewFirst_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewLast_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewFiltered_Click(object sender, RoutedEventArgs e)
        {

        }

        public ItemsControl GetTreeViewItemParent(TreeViewItem item)
        {
            var parent = VisualTreeHelper.GetParent(item);
            while (!(parent is TreeViewItem || parent is TreeView))
                parent = VisualTreeHelper.GetParent(parent);

            return parent as ItemsControl;
        }
    }
}
