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
using NpgsqlTypes;

namespace DbCourseWork
{
    public class PostgreCommunication
    {
        public DataSet Ds { get; }
        private NpgsqlDataAdapter Adapter { get; set; }
        private NpgsqlConnectionStringBuilder ConnectionBuilder { get; set; }

        public PostgreCommunication(string dbName, string userId, string server, string port, string password)
        {
            ConnectionBuilder = CreateConnectionStringBuilder(server, port, dbName, userId, password);

            Ds = new DataSet(dbName);
        }

        public PostgreCommunication(ConnectionStringSettings connectionString)
        {
            ConnectionBuilder = new NpgsqlConnectionStringBuilder(connectionString.ConnectionString);

            Ds = new DataSet(connectionString.Name);
        }

        public static NpgsqlConnectionStringBuilder CreateConnectionStringBuilder(string server, string port, string dbName, string userId, string password)
        {
            var connectionBuilder = new NpgsqlConnectionStringBuilder(
                "Server=" + server + ";Port=" + port + ";")
            {
                {"Database", dbName},
                {"User Id", userId}
            };
            connectionBuilder.Password = password;

            return connectionBuilder;
        }

        public PostgreCommunication(string connectionString, string dbName)
        {
            ConnectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            Ds = new DataSet(dbName);
        }

        public async Task ShowOnDataGridAsync(DataTable table)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionBuilder.ConnectionString))
                {
                    var sqlExpression = "SELECT * FROM " + table.TableName;
                    await connection.OpenAsync();

                    Adapter = new NpgsqlDataAdapter(sqlExpression, connection);
                    Ds.Tables[table.TableName].Clear();
                    Adapter.Fill(Ds, table.TableName);
                    MainWindow.CustomGrid.DataContext = Ds.Tables[table.TableName];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ShowOnDataGridAsync(DataTable table, int limit, bool desc = false)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionBuilder.ConnectionString))
                {
                    string sqlExpression;
                    if (!desc) sqlExpression = "SELECT * FROM " + table.TableName + " LIMIT " + limit;
                    else sqlExpression = "SELECT * FROM (SELECT * FROM " + table.TableName + 
                                         " ORDER BY " + table.Columns[0].ColumnName + " DESC LIMIT " + 
                                         limit + ")" + " AS " + table.TableName +
                                         " ORDER BY " + table.TableName + " ASC";

                    await connection.OpenAsync();
                    Adapter = new NpgsqlDataAdapter(sqlExpression, connection);
                    Ds.Tables[table.TableName].Clear();
                    Adapter.Fill(Ds, table.TableName);

                    MainWindow.CustomGrid.DataContext = Ds.Tables[table.TableName];

                    for (var i = 0; i < table.Columns.Count; i++)
                        if (table.Columns[i].AutoIncrement)
                            MainWindow.CustomGrid.Columns[i].IsReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task UpdateDbAsync(DataTable table)
        {
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionBuilder.ConnectionString))
                {
                    await connection.OpenAsync();
                    Adapter = new NpgsqlDataAdapter("SELECT * FROM " + table.TableName, connection);
                    var commandBuilder = new NpgsqlCommandBuilder(Adapter);

                    var insertString = table.Columns.Cast<DataColumn>()
                        .Aggregate("INSERT INTO " + table.TableName + " (", (current, column) => current + column.ColumnName + ", ");
                    insertString = insertString.Remove(insertString.Length - 2, 2);
                    insertString += ") ";

                    insertString = table.Columns.Cast<DataColumn>()
                        .Aggregate(insertString + "VALUES (", (current, column) => current + "@" + column.ColumnName + ", ");
                    insertString = insertString.Remove(insertString.Length - 2, 2);
                    insertString += ")";


                    var insertCommand = new NpgsqlCommand(insertString);

                    foreach (DataColumn column in table.Columns)
                    {
                        var type = column.DataType;
                        insertCommand.Parameters.Add("@" + column.ColumnName, NpgsqlHelper.GetDbType(column.DataType),
                            column.MaxLength, column.ColumnName);
                    }

                    Adapter.InsertCommand = insertCommand;

                    var countRows = Adapter.Update(table);
                    MessageBox.Show(countRows + " changes were saved in the database.", 
                        "Update rows", MessageBoxButton.OK, MessageBoxImage.Information);
                    table.Clear();
                    Adapter.Fill(table);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            "SELECT DISTINCT                                        " +
                            "a.attnum as num,                                       " +
                            "a.attname as name,                                     " +
                            "format_type(a.atttypid, a.atttypmod) as typ,           " +
                            "a.attnotnull as notnull,                               " +
                            "coalesce(i.indisprimary, false) as primary_key         " +
                            "FROM pg_attribute a                                    " +
                            "    JOIN pg_class pgc ON pgc.oid = a.attrelid          " +
                            "LEFT JOIN pg_index i ON                                " +
                            "    (pgc.oid = i.indrelid AND i.indkey[0] = a.attnum)  " +
                            "LEFT JOIN pg_description com on                        " +
                            "    (pgc.oid = com.objoid AND a.attnum = com.objsubid) " +
                            "LEFT JOIN pg_attrdef def ON                            " +
                            "    (a.attrelid = def.adrelid AND a.attnum = def.adnum)" +
                            "WHERE a.attnum > 0 AND pgc.oid = a.attrelid            " +
                            "AND pg_table_is_visible(pgc.oid)                       " +
                            "AND NOT a.attisdropped                                 " +
                            "    AND pgc.relname = '" + table.TableName + "'        " + 
                            "ORDER BY a.attnum";
                        command.CommandText = sqlExpression;
                        reader = command.ExecuteReader();
                        if (!reader.HasRows) continue;
                        while (reader.Read())
                        {
                            var column = new DataColumn(reader.GetValue(1).ToString())
                            { DataType = NpgsqlHelper.TypeFromPostgresType(reader.GetValue(2).ToString()) };
                            if (column.DataType.Name == "String")
                            {
                                var maxLength = NpgsqlHelper.GetMaxLength(reader.GetValue(2).ToString());
                                if (maxLength != -1)
                                    column.MaxLength = maxLength;
                            }
                            //column.AllowDBNull = (bool) reader.GetValue(3);

                            table.Columns.Add(column);
                        }


                        Ds.Tables.Add(table);

                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
