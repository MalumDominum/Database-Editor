using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DbCourseWork
{
    public class PostgreCommunication
    {
        public DataSet Ds { get; }
        private NpgsqlDataAdapter Adapter { get; set; }
        private NpgsqlConnectionStringBuilder ConnectionBuilder { get; set; }

        public PostgreCommunication(string dbName, string userId, string server, string port, string password)
        {
            ConnectionBuilder = new NpgsqlConnectionStringBuilder("Server=" + server + ";Port=" + port + ";")
            {
                {"Database", dbName},
                {"User Id", userId}
            };
            ConnectionBuilder.Password = password;

            Ds = new DataSet(dbName);
        }

        public PostgreCommunication(ConnectionStringSettings connectionString)
        {
            ConnectionBuilder = new NpgsqlConnectionStringBuilder(connectionString.ConnectionString);

            Ds = new DataSet(connectionString.Name);
        }

        public PostgreCommunication(string connectionString, string dbName)
        {
            ConnectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            Ds = new DataSet(dbName);
        }

        public async Task ShowOnDataGrid(DataTable table)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionBuilder.ConnectionString))
                {
                    var sqlExpression = "SELECT * FROM " + table.TableName;
                    await connection.OpenAsync();
                    Adapter = new NpgsqlDataAdapter(sqlExpression, connection);
                    Adapter.Fill(Ds, table.TableName);
                    MainWindow.CustomGrid.DataContext = Ds.Tables[table.TableName];
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async Task UpdateDb(DataTable table)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionBuilder.ConnectionString))
                {
                    await connection.OpenAsync();
                    var commandBuilder = new NpgsqlCommandBuilder(Adapter);
                    Adapter.Update(Ds);
                    Ds.Clear();
                    Adapter.Fill(Ds);
                    MainWindow.CustomGrid.DataContext = Ds.Tables["persons"];
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void FillDs()
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionBuilder.ConnectionString))
                {
                    connection.Open();
                    var sqlExpression = // Select all table names
                        "SELECT table_name " +
                        "FROM information_schema.tables " +
                        "WHERE table_schema = 'public' " +
                        "AND table_type = 'BASE TABLE'";
                    var command = new NpgsqlCommand(sqlExpression, connection);
                    var reader = command.ExecuteReader();

                    var tableNames = new List<string>();
                    if (reader.HasRows)
                        while (reader.Read())
                            tableNames.Add(reader.GetValue(0).ToString());
                    reader.Close();
                    foreach (var table in tableNames.Select(tableName => new DataTable(tableName)))
                    {
                        sqlExpression = // Select all column names
                            "SELECT column_name                          " +
                            "FROM information_schema.columns             " +
                            "WHERE table_name = '" + table.TableName + "'" +
                            "ORDER BY ordinal_position";
                        command.CommandText = sqlExpression;
                        reader = command.ExecuteReader();
                        if (!reader.HasRows) continue;
                        while (reader.Read())
                            table.Columns.Add(new DataColumn(reader.GetValue(0).ToString()));

                        Ds.Tables.Add(table);

                        reader.Close();
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
