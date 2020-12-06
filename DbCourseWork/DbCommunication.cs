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
    public class DbCommunication
    {
        public DataSet Ds { get; set; }
        public NpgsqlDataAdapter Adapter { get; set; }
        public NpgsqlCommandBuilder Builder { get; set; }
        public Dictionary<string, DataBase> DataBases { get; set; } = new Dictionary<string, DataBase>();

        public DbCommunication()
        {
            for (int i = 1; i < ConfigurationManager.ConnectionStrings.Count; i++)
                DataBases.Add(ConfigurationManager.ConnectionStrings[i].Name,
                    new DataBase(new NpgsqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[i].ConnectionString)) );
        }

        public async Task ConnectionToPostgres()
        {
            //@"Server=localhost;Port=5433;Database=streamingservice;User Id=postgres;Password=c788f928bd244746ae0ca3c79c3488a8;"
            // For Ms SQL use SqlConnectionStringBuilder
            var connectionBuilder = new NpgsqlConnectionStringBuilder("Server=localhost;Port=5433;")
            {
                {"Database", "streamingservice"}, 
                {"User Id", "postgres"}
            };
            connectionBuilder.Password = "c788f928bd244746ae0ca3c79c3488a8";

            try
            {
                using (var connection = new NpgsqlConnection(connectionBuilder.ConnectionString))
                {
                    await connection.OpenAsync();
                    var sqlExpression = "INSERT INTO persons (first_name, last_name, gender, birth_date, height)" +
                    "VALUES('Райан', 'Гослинг', 'м', date '1980-11-12', 1.84)";
                    var command = new NpgsqlCommand(sqlExpression, connection);
                    int number = await command.ExecuteNonQueryAsync();
                    MessageBox.Show("Добавлено объектов:" + number);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async Task SelectPersons()
        {
            var builder = new NpgsqlConnectionStringBuilder("Server=localhost;Port=5433;")
            {
                {"Database", "streamingservice"},
                {"User Id", "postgres"}
            };
            builder.Password = "c788f928bd244746ae0ca3c79c3488a8";

            try
            {
                using (var connection = new NpgsqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    var sqlExpression = "SELECT * FROM persons";
                    var command = new NpgsqlCommand(sqlExpression, connection);
                    var reader = await command.ExecuteReaderAsync();
                    if (reader.HasRows)
                    {
                        var rowNames = "";
                        for (int i = 0; i < reader.FieldCount; i++)
                            rowNames += reader.GetName(i) + " ";
                        MessageBox.Show(rowNames);

                        while (await reader.ReadAsync())
                        {
                            var row = "";
                            for (int i = 0; i < reader.FieldCount; i++)
                                row += reader.GetValue(i) + " ";
                            MessageBox.Show(row);
                        }
                    }
                    reader.Close();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async Task ShowOnDataGrid()
        {
            var builder = new NpgsqlConnectionStringBuilder("Server=localhost;Port=5433;")
            {
                {"Database", "streamingservice"},
                {"User Id", "postgres"}
            };
            builder.Password = "c788f928bd244746ae0ca3c79c3488a8";

            try
            {
                using (var connection = new NpgsqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    var sqlExpression = "SELECT * FROM persons";
                    Adapter = new NpgsqlDataAdapter(sqlExpression, connection);
                    Ds = new DataSet();
                    Adapter.Fill(Ds);
                    MainWindow.CustomGrid.DataContext = Ds.Tables[0];
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public async Task UpdateDb(string table)
        {
            var builder = new NpgsqlConnectionStringBuilder("Server=localhost;Port=5433;")
            {
                {"Database", "streamingservice"},
                {"User Id", "postgres"}
            };
            builder.Password = "c788f928bd244746ae0ca3c79c3488a8";

            try
            {
                using (var connection = new NpgsqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    Adapter.Update(MainWindow.CustomGrid.DataContext as DataTable
                                   ?? throw new InvalidOperationException("You don't choose a table"));
                    Ds.Clear();
                    Adapter.Fill(Ds);
                    MainWindow.CustomGrid.DataContext = Ds.Tables[0];
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public DataBase AddDbTableNames(DataBase db)
        {
            var builder = db.ConnectionBuilder;
            try
            {
                using (var connection = new NpgsqlConnection(builder.ConnectionString))
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

                    db.TableNames = tableNames;

                    reader.Close();
                    return db;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
                return db;
            }
        }
    }
}
