using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Npgsql;

namespace DbCourseWork
{
    public partial class MainWindow
    {
        enum SqlServer { MsSql, PostgreSql }
        public static DataGrid CustomGrid { get; private set; }
        private static Dictionary<string, NpgsqlCommunication> PostgreCommunications { get; } = new Dictionary<string, NpgsqlCommunication>();
        private static Dictionary<string, MsCommunication> MsCommunications { get; } = new Dictionary<string, MsCommunication>();
        private static SqlServer CurrentServer { get; set; }
        private static DataTable CurrentTable { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            CustomGrid = DataTableGrid;

            var connectionStrings = ConfigurationManager.ConnectionStrings;
            foreach (ConnectionStringSettings connectionString in connectionStrings)
            {
                switch (connectionString.ProviderName)
                {
                    case "PostgreSQL":
                        PostgreCommunications.Add(connectionString.Name, new NpgsqlCommunication(connectionString));
                        PostgreCommunications[connectionString.Name].FillDs();
                        BuildTreeView(DatabasesTreeView, PostgreCommunications[connectionString.Name]);
                        break;
                    case "MS SQL":
                        MsCommunications.Add(connectionString.Name, new MsCommunication(connectionString));
                        MsCommunications[connectionString.Name].FillDs();
                        BuildTreeView(DatabasesTreeView, MsCommunications[connectionString.Name]);
                        break;
                }
            }
        }

        private void SaveChangesButton_OnClick(object sender, RoutedEventArgs e)
        {
            PostgreCommunications[CurrentTable.DataSet.DataSetName].UpdateDbAsync(CurrentTable).GetAwaiter();
        }

        private void ReturnChangesButton_OnClick(object sender, RoutedEventArgs e)
        {
            PostgreCommunications[CurrentTable.DataSet.DataSetName].ShowOnDataGridAsync(CurrentTable).GetAwaiter();
        }

        private void AddDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NpgsqlConnectionStringBuilder connectionBuilder;
                var addDbWindow = new AddDbWindow();
                if (addDbWindow.ShowDialog() == true)
                {
                    connectionBuilder = NpgsqlCommunication.CreateConnectionStringBuilder(
                        addDbWindow.Server, addDbWindow.Port, addDbWindow.Database,
                        addDbWindow.User, addDbWindow.Password);

                    var connection = new NpgsqlConnection(connectionBuilder.ConnectionString);
                    connection.Open();

                    if (connection.State == ConnectionState.Open)
                        MessageBox.Show("The connection to " + addDbWindow.Database + " was successful. Database saved",
                            "Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                    {
                        MessageBox.Show("Failed to connect to database, maybe server is closed or your input is wrong.",
                            "Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    connection.Close();
                }
                else return;

                var communication =
                    new NpgsqlCommunication(addDbWindow.Database, addDbWindow.User,
                        addDbWindow.Server, addDbWindow.Port, addDbWindow.Password);

                // Create recording in the DbCourseWork.exe.Config
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.ConnectionStrings.ConnectionStrings;
                if (settings[addDbWindow.Database] == null)
                {
                    settings.Add(new ConnectionStringSettings(addDbWindow.Database, connectionBuilder.ConnectionString));
                    settings[addDbWindow.Database].ProviderName = "PostgreSQL";

                    PostgreCommunications.Add(addDbWindow.Database, communication);
                    PostgreCommunications[addDbWindow.Database].FillDs();

                    BuildTreeView(DatabasesTreeView, communication);
                }
                
                else
                {
                    settings[addDbWindow.Database].Name = addDbWindow.Database;
                    settings[addDbWindow.Database].ConnectionString = connectionBuilder.ConnectionString;
                    settings[addDbWindow.Database].ProviderName = "PostgreSQL";

                    PostgreCommunications.Remove(addDbWindow.Database);
                    PostgreCommunications.Add(addDbWindow.Database, communication);
                    PostgreCommunications[addDbWindow.Database].FillDs();

                    RemoveTreeView(DatabasesTreeView, addDbWindow.Database);
                    BuildTreeView(DatabasesTreeView, communication);
                }
                
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("connectionStrings");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildTreeView(TreeView treeView, MsCommunication communication) { }

        private void BuildTreeView(TreeView treeView, NpgsqlCommunication communication)
        {
            var dbContextMenu = new ContextMenu();
            var dbRefreshItem = new MenuItem { Header = "Refresh" };
            dbRefreshItem.Click += DbMenuItem_Refresh;
            dbContextMenu.Items.Add(dbRefreshItem);

            var dbDisconnectItem = new MenuItem { Header = "Disconnect" };
            dbDisconnectItem.Click += DbMenuItem_Disconnect;
            dbContextMenu.Items.Add(dbDisconnectItem);


            var dbView = new TreeViewItem
            {
                Header = communication.Ds.DataSetName,
                ContextMenu = dbContextMenu
            };
            dbView.ContextMenu.PlacementTarget = dbView;
            
            dbView.Header = CreateHeaderWithIcon("Database", dbView.Header.ToString(), 
                new Thickness(0, 0, 0, 3), new Thickness(5, 0, 0, 0));
            
            treeView.Items.Add(dbView);

            foreach (DataTable table in communication.Ds.Tables)
            {
                var tableContextMenu = new ContextMenu();
                var tableViewAllItem = new MenuItem { Header = "View all rows" };
                tableViewAllItem.Click += ViewAll_Click;
                tableContextMenu.Items.Add(tableViewAllItem);

                var tableViewFirstItem = new MenuItem { Header = "View first 100 rows" };
                tableViewFirstItem.Click += ViewFirst_Click;
                tableContextMenu.Items.Add(tableViewFirstItem);

                var tableViewLastItem = new MenuItem { Header = "View last 100 rows" };
                tableViewLastItem.Click += ViewLast_Click;
                tableContextMenu.Items.Add(tableViewLastItem);

                var tableView = new TreeViewItem
                {
                    Header = table.TableName,
                    ContextMenu = tableContextMenu
                };
                tableView.ContextMenu.PlacementTarget = tableView;
                tableView.Header = CreateHeaderWithIcon("Table", tableView.Header.ToString(), 
                    new Thickness(0), new Thickness(5, 0, 0, 5));
                dbView.Items.Add(tableView);

                foreach (DataColumn column in table.Columns)
                {
                    var columnView = new TreeViewItem { Header = column.ColumnName };
                    columnView.Header = CreateHeaderWithIcon("Column", columnView.Header.ToString(), 
                        new Thickness(0), new Thickness(0, 0, 0, 2));
                    tableView.Items.Add(columnView);
                }
            }
        }

        private void RemoveTreeView(TreeView treeView, string viewHeader)
        {
            for (int i = 0; i < treeView.Items.Count; i++)
            {
                var viewItem = (TreeViewItem) treeView.Items[i];
                if (!(viewItem.Header is StackPanel stack)) return;

                var header = (TextBlock)stack.Children[1];
                if (header.Text == viewHeader)
                    treeView.Items.Remove(viewItem);
            }
        }

        private StackPanel CreateHeaderWithIcon(string icon, string headerText, Thickness iconThickness, Thickness textThickness)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            /*var database = (Geometry) FindResource(icon);
            var canvas = new Canvas() {Width = 22, Height = 22, Margin = new Thickness(5)};
            var path = new Path() {Width = 22, Height = 22, Stretch = Stretch.Fill, 
                    Fill = (Brush) FindResource("FillPath"), Data = database};
            canvas.Children.Add(path);*/

            var database = (Canvas)FindResource(icon);
            database = (Canvas)XamlReader.Parse(XamlWriter.Save(database));
            database.Width = 22;
            database.Height = 22;
            var contentControl = new ContentControl
            {
                Content = database,
                Width = 22,
                Height = 22,
                Margin = iconThickness
            };

            var text = new TextBlock
            {
                Text = headerText,
                Margin = textThickness
            };

            stack.Children.Add(contentControl);
            stack.Children.Add(text);

            return stack;
        }

        private void DbMenuItem_Refresh(object sender, RoutedEventArgs e)
        {
            
        }

        private void DbMenuItem_Disconnect(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem)) return;

            if (!(menuItem.Parent is ContextMenu contextMenu)) return;

            if (!(contextMenu.PlacementTarget is TreeViewItem viewItem)) return;

            if (!(viewItem.Header is StackPanel stack)) return;

            DatabasesTreeView.Items.Remove(viewItem);

            var header = (TextBlock)stack.Children[1];
            PostgreCommunications.Remove(header.Text);

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.ConnectionStrings.ConnectionStrings.Remove(header.Text);
            config.Save();
        }

        private void ViewAll_Click(object sender, RoutedEventArgs e)
        {
            View(sender);
        }

        private void ViewFirst_Click(object sender, RoutedEventArgs e)
        {
            View(sender, true, 100);
        }

        private void ViewLast_Click(object sender, RoutedEventArgs e)
        {
            View(sender, true, 100, true);
        }

        private void EnableElementsOnView()
        {
            SaveChangesButton.IsEnabled = true;
            ReturnChangesButton.IsEnabled = true;
            PaginationComboBox.IsEnabled = true;
            ColumnsComboBox.IsEnabled = true;
        }

        private void View(object sender, bool useLimit = false, int limit = 0, bool desc = false)
        {
            if (!(sender is MenuItem menuItem)) return;

            if (!(menuItem.Parent is ContextMenu contextMenu)) return;

            if (!(contextMenu.PlacementTarget is TreeViewItem viewItem)) return;

            if (!(viewItem.Header is StackPanel stack)) return;

            if (!(viewItem.Parent is TreeViewItem parentView)) return;

            if (!(parentView.Header is StackPanel parentStack)) return;

            EnableElementsOnView();

            var dbHeader = (TextBlock) parentStack.Children[1];
            var tableHeader = (TextBlock) stack.Children[1];
            CurrentTable = PostgreCommunications[dbHeader.Text].Ds.Tables[tableHeader.Text];
            var currentTable = CurrentTable.Copy();

            if (!useLimit) PostgreCommunications[dbHeader.Text].ShowOnDataGridAsync(currentTable).GetAwaiter();
            else PostgreCommunications[dbHeader.Text].ShowOnDataGridAsync(currentTable, limit, desc).GetAwaiter();

            PageTextBox.Text = "1";
            PageCountTextBlock.Text = ((CurrentTable.Rows.Count / Convert.ToInt32(RowsPerPageTextBox.Text)) + 1).ToString();

            if (PaginationComboBox.IsChecked != null && (bool) PaginationComboBox.IsChecked)
                TurnOverPage(0);

            var list = new ObservableCollection<string>();
            foreach (DataColumn column in CurrentTable.Columns)
                list.Add(column.ColumnName);
            ColumnsComboBox.ItemsSource = list;

            VisibilitySearchElementsChange(Visibility.Collapsed);
        }

        private void PaginationPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            TurnOverPage(-1);
        }

        private void PaginationNextButton_Click(object sender, RoutedEventArgs e)
        {
            TurnOverPage(1);
        }

        private void TurnOverPage(int value)
        {
            for (int i = 0; i < PageTextBox.Text.Length; i++)
                if (!char.IsNumber(PageTextBox.Text[i]))
                    PageTextBox.Text = PageTextBox.Text.Remove(i--, 1);

            for (int i = 0; i < RowsPerPageTextBox.Text.Length; i++)
                if (!char.IsNumber(RowsPerPageTextBox.Text[i]))
                    RowsPerPageTextBox.Text = RowsPerPageTextBox.Text.Remove(i--, 1);

            var result = Convert.ToInt32(PageTextBox.Text) + value;
            var pagesCount = Convert.ToInt32(PageCountTextBlock.Text);
            var rowsPerPage = Convert.ToInt32(RowsPerPageTextBox.Text);

            if (result <= 0 || result > pagesCount) return;

            PageTextBox.Text = (result).ToString();

            var pagedTable = CurrentTable.Clone();
            for (int i = 0; i < rowsPerPage; i++)
            {
                var newRow = pagedTable.NewRow();
                var number = (result - 1) * rowsPerPage + i;
                if (number >= CurrentTable.Rows.Count) continue;
                newRow.ItemArray = CurrentTable.Rows[number].ItemArray;

                pagedTable.Rows.Add(newRow);
            }

            DataTableGrid.DataContext = pagedTable;
        }

        private void RowsPerPageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                PageCountTextBlock.Text = ((CurrentTable.Rows.Count / Convert.ToInt32(RowsPerPageTextBox.Text)) + 1).ToString();
                TurnOverPage(0);
            }
            catch
            {
                // ignore
            }
        }

        private void PageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TurnOverPage(0);
            }
            catch
            {
                // ignore
            }
        }

        private void PaginationComboBox_Checked(object sender, RoutedEventArgs e)
        {
            EnableElementsOnView(true);
            ReturnChangesButton.IsEnabled = false;
            SaveChangesButton.IsEnabled = false;
            CustomGrid.CanUserAddRows = false;
            TurnOverPage(0);
        }

        private void PaginationComboBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            EnableElementsOnView(false);
            ReturnChangesButton.IsEnabled = true;
            SaveChangesButton.IsEnabled = true;
            CustomGrid.CanUserAddRows = true;
            DataTableGrid.DataContext = CurrentTable;
        }

        private void EnableElementsOnView(bool enable)
        {
            PaginationPreviousButton.IsEnabled = enable;
            PaginationNextButton.IsEnabled = enable;
            PageTextBox.IsEnabled = enable;
            RowsPerPageTextBox.IsEnabled = enable;
            if (enable)
            {
                RowsPerPageTextBlock.Foreground = (SolidColorBrush)FindResource("ControlForegroundWhite");
                PageCountTextBlock.Foreground = (SolidColorBrush)FindResource("ControlForegroundWhite");
                InclinedTextBlock.Foreground = (SolidColorBrush)FindResource("ControlForegroundWhite");
            }
            else
            {
                RowsPerPageTextBlock.Foreground = (SolidColorBrush)FindResource("DisabledForeground2");
                PageCountTextBlock.Foreground = (SolidColorBrush)FindResource("DisabledForeground2");
                InclinedTextBlock.Foreground = (SolidColorBrush)FindResource("DisabledForeground2");
            }
        }

        private void ColumnsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VisibilitySearchElementsChange(Visibility.Collapsed);
            var columnName = ((ComboBox) sender).SelectedItem as string;
            DataColumn currentColumn = null;
            foreach (DataColumn column in CurrentTable.Columns)
                if (column.ColumnName == columnName)
                    currentColumn = column;

            switch (currentColumn?.DataType.Name)
            {
                case "DateTime":
                    FilterFirstDate.Visibility = Visibility.Visible;
                    FilterSecondDate.Visibility = Visibility.Visible;
                    break;
                case "TimeSpan":
                    FilterFirstInterval.Visibility = Visibility.Visible;
                    FilterSecondInterval.Visibility = Visibility.Visible;
                    break;
                default:
                    BaseSearchElement.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void VisibilitySearchElementsChange(Visibility visibility)
        {
            ColumnsComboBox.Text = "";
            BaseSearchElement.Visibility = visibility;
            FilterFirstDate.Visibility = visibility;
            FilterSecondDate.Visibility = visibility;
            FilterFirstInterval.Visibility = visibility;
            FilterSecondInterval.Visibility = visibility;
            BaseSearchTextBox.Text = "";
            FilterFirstDate.Text = "";
            FilterSecondDate.Text = "";
            FilterFirstInterval.Text = "";
            FilterSecondInterval.Text = "";
        }

        private void BasicSearch(string text)
        {
            if (text == "")
            {
                CustomGrid.DataContext = CurrentTable;
                return;
            }

            var currentColumnIndex = 0;
            for (int i = 0; i < CurrentTable.Columns.Count; i++)
                if (CurrentTable.Columns[i].ColumnName == ColumnsComboBox.Text)
                    currentColumnIndex = i;

            var foundedRows = CurrentTable.Select()
                .Where(r => r.ItemArray[currentColumnIndex].ToString().Contains(text));

            var afterSearchTable = CurrentTable.Clone();
            foreach (var row in foundedRows)
            {
                var newRow = afterSearchTable.NewRow();
                newRow.ItemArray = row.ItemArray;
                afterSearchTable.Rows.Add(newRow);
            }

            CustomGrid.DataContext = afterSearchTable;
        }
        private void BaseSearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            BasicSearch(((TextBox)sender).Text);
        }

        private void FilterFirstDate_OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var text = ((DatePicker)sender).Text;
            if (text == "" && FilterSecondDate.Text == "")
            {
                CustomGrid.DataContext = CurrentTable;
                return;
            }
            if (FilterSecondDate.Text == "")
            {
                BasicSearch(((DatePicker)sender).Text);
                return;
            }

            var currentColumnIndex = 0;
            for (int i = 0; i < CurrentTable.Columns.Count; i++)
                if (CurrentTable.Columns[i].ColumnName == ColumnsComboBox.Text)
                    currentColumnIndex = i;

            try
            {
                var foundedRows = CurrentTable.Select()
                    .Where(r => Convert.ToDateTime(r.ItemArray[currentColumnIndex]) >= Convert.ToDateTime(text) &&
                                Convert.ToDateTime(r.ItemArray[currentColumnIndex]) <= Convert.ToDateTime(FilterSecondDate.Text));

                var afterSearchTable = CurrentTable.Clone();
                foreach (DataRow row in foundedRows)
                {
                    var newRow = afterSearchTable.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    afterSearchTable.Rows.Add(newRow);
                }

                CustomGrid.DataContext = afterSearchTable;
            }
            catch
            {
                // ignore
            }
        }
        private void FilterSecondDate_OnSelectedDateChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            var text = ((DatePicker)sender).Text;
            if (text == "" && FilterFirstDate.Text == "")
            {
                CustomGrid.DataContext = CurrentTable;
                return;
            }
            if (FilterFirstDate.Text == "")
            {
                BasicSearch(((DatePicker)sender).Text);
                return;
            }

            var currentColumnIndex = 0;
            for (int i = 0; i < CurrentTable.Columns.Count; i++)
                if (CurrentTable.Columns[i].ColumnName == ColumnsComboBox.Text)
                    currentColumnIndex = i;

            try
            {
                var foundedRows = CurrentTable.Select()
                    .Where(r => Convert.ToDateTime(r.ItemArray[currentColumnIndex]) <= Convert.ToDateTime(text) &&
                                Convert.ToDateTime(r.ItemArray[currentColumnIndex]) >= Convert.ToDateTime(FilterFirstDate.Text));

                var afterSearchTable = CurrentTable.Clone();
                foreach (DataRow row in foundedRows)
                {
                    var newRow = afterSearchTable.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    afterSearchTable.Rows.Add(newRow);
                }

                CustomGrid.DataContext = afterSearchTable;
            }
            catch
            {
                // ignore
            }
        }

        private void FilterFirstInterval_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text;
            if (text == "" && FilterSecondInterval.Text == "")
            {
                CustomGrid.DataContext = CurrentTable;
                return;
            }
            if (FilterSecondInterval.Text == "")
            {
                BasicSearch(((TextBox)sender).Text);
                return;
            }

            var currentColumnIndex = 0;
            for (int i = 0; i < CurrentTable.Columns.Count; i++)
                if (CurrentTable.Columns[i].ColumnName == ColumnsComboBox.Text)
                    currentColumnIndex = i;

            try
            {
                var foundedRows = CurrentTable.Select()
                    .Where(r => TimeSpan.Parse(r.ItemArray[currentColumnIndex].ToString()) >= TimeSpan.Parse(text) &&
                                TimeSpan.Parse(r.ItemArray[currentColumnIndex].ToString()) <= TimeSpan.Parse(FilterSecondInterval.Text));

                var afterSearchTable = CurrentTable.Clone();
                foreach (DataRow row in foundedRows)
                {
                    var newRow = afterSearchTable.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    afterSearchTable.Rows.Add(newRow);
                }

                CustomGrid.DataContext = afterSearchTable;
            }
            catch
            {
                // ignore
            }
        }

        private void FilterSecondInterval_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text;
            if (text == "" && FilterFirstInterval.Text == "")
            {
                CustomGrid.DataContext = CurrentTable;
                return;
            }
            if (FilterFirstInterval.Text == "")
            {
                BasicSearch(((TextBox)sender).Text);
                return;
            }

            var currentColumnIndex = 0;
            for (int i = 0; i < CurrentTable.Columns.Count; i++)
                if (CurrentTable.Columns[i].ColumnName == ColumnsComboBox.Text)
                    currentColumnIndex = i;

            try
            {
                var foundedRows = CurrentTable.Select()
                    .Where(r => TimeSpan.Parse(r.ItemArray[currentColumnIndex].ToString()) <= TimeSpan.Parse(text) &&
                                TimeSpan.Parse(r.ItemArray[currentColumnIndex].ToString()) >= TimeSpan.Parse(FilterFirstInterval.Text));

                var afterSearchTable = CurrentTable.Clone();
                foreach (DataRow row in foundedRows)
                {
                    var newRow = afterSearchTable.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    afterSearchTable.Rows.Add(newRow);
                }

                CustomGrid.DataContext = afterSearchTable;
            }
            catch
            {
                // ignore
            }
        }
        private void DataTableGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = ((DataRowView)DataTableGrid.SelectedItem).Row;
                SelectedRowBarItem.Content = "Selected row: " + (selectedItem.Table.Rows.IndexOf(selectedItem) + 1);
            }
            catch
            {
                SelectedRowBarItem.Content = "";
            }
        }
    }
}
