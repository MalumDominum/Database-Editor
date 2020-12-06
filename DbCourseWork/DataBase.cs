using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace DbCourseWork
{
    public class DataBase
    {
        public NpgsqlConnectionStringBuilder ConnectionBuilder { get; set; }
        public List<string> TableNames { get; set; } = new List<string>();
        public DataBase(NpgsqlConnectionStringBuilder connectionBuilder)
        {
            ConnectionBuilder = connectionBuilder;
        }
    }
}
