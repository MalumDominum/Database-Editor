using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NpgsqlTypes;

namespace DbCourseWork
{
    public static class NpgsqlHelper
    {
        private static readonly Dictionary<Type, NpgsqlDbType> TypeToNpgsqlDb;
        private static readonly Dictionary<string, Type> PostgreTypeToType;

        // Create and populate the dictionary in the static constructor
        static NpgsqlHelper()
        {
            TypeToNpgsqlDb = new Dictionary<Type, NpgsqlDbType>
            {
                [typeof(string)] = NpgsqlDbType.Varchar,
                [typeof(char[])] = NpgsqlDbType.Varchar,
                [typeof(byte)] = NpgsqlDbType.Bytea,
                [typeof(short)] = NpgsqlDbType.Smallint,
                [typeof(int)] = NpgsqlDbType.Integer,
                [typeof(long)] = NpgsqlDbType.Bigint,
                [typeof(byte[])] = NpgsqlDbType.Bytea,
                [typeof(bool)] = NpgsqlDbType.Bit,
                [typeof(DateTime)] = NpgsqlDbType.Timestamp,
                [typeof(DateTimeOffset)] = NpgsqlDbType.TimestampTz,
                [typeof(decimal)] = NpgsqlDbType.Money,
                [typeof(float)] = NpgsqlDbType.Real,
                [typeof(double)] = NpgsqlDbType.Numeric,
                [typeof(TimeSpan)] = NpgsqlDbType.Time
            };


            PostgreTypeToType = new Dictionary<string, Type>
            {
                ["boolean"] = typeof(bool),
                ["smallint"] = typeof(short),
                ["integer"] = typeof(int),
                ["bigint"] = typeof(long),
                ["real"] = typeof(float),
                ["double precision"] = typeof(double),
                ["numeric"] = typeof(decimal),
                ["money"] = typeof(decimal),
                ["text"] = typeof(string),
                ["character varying"] = typeof(string),
                ["character"] = typeof(string),
                ["bpchar"] = typeof(string),
                ["citext"] = typeof(string),
                ["json"] = typeof(string),
                ["jsonb"] = typeof(string),
                ["xml"] = typeof(string),
                ["bit varying"] = typeof(BitArray),
                ["date"] = typeof(DateTime),
                ["interval"] = typeof(TimeSpan),
                ["timestamp"] = typeof(DateTime),
                ["timestamp without time zone"] = typeof(DateTime),
                ["timestamp with time zone"] = typeof(DateTime),
                ["time"] = typeof(TimeSpan),
                ["time without time zone"] = typeof(TimeSpan),
                ["time with time zone"] = typeof(DateTimeOffset),
                ["bytea"] = typeof(byte[]),
                ["oid"] = typeof(uint),
                ["cid"] = typeof(uint),
                ["xid"] = typeof(uint),
                ["oidvector"] = typeof(uint[]),
                ["name"] = typeof(string),
                ["record"] = typeof(object[]),
                ["enum types"] = typeof(Enum),
                ["point"] = typeof(NpgsqlPoint),
                ["lseg"] = typeof(NpgsqlLSeg),
                ["path"] = typeof(NpgsqlPath),
                ["polygon"] = typeof(NpgsqlPolygon),
                ["line"] = typeof(NpgsqlLine),
                ["circle"] = typeof(NpgsqlCircle),
                ["box"] = typeof(NpgsqlBox),
                ["hstore"] = typeof(Dictionary<string, string>),
                ["macaddr"] = typeof(PhysicalAddress),
                ["tsquery"] = typeof(NpgsqlTsQuery),
                ["tsvector"] = typeof(NpgsqlTsVector),
                ["inet"] = typeof(IPAddress),
                ["uuid"] = typeof(Guid),
                ["array"] = typeof(Array)
            };
        }

        public static NpgsqlDbType PostgresTypeFromType(Type giveType)
        {
            giveType = Nullable.GetUnderlyingType(giveType) ?? giveType;

            if (TypeToNpgsqlDb.ContainsKey(giveType))
                return TypeToNpgsqlDb[giveType];

            throw new ArgumentException($"{giveType.FullName} is not a supported .NET class");
        }

        public static NpgsqlDbType PostgresTypeFromType<T>()
        {
            return PostgresTypeFromType(typeof(T));
        }

        public static int GetMaxLength(string typeString)
        {
            if (!typeString.EndsWith(")")) return -1;

            var number = "";
            var numbers = new List<int>();
            foreach (var chr in typeString)
            {
                if (char.IsDigit(chr))
                    number += chr;

                else if (number != "")
                {
                    numbers.Add(Convert.ToInt32(number));
                    number = "";
                }
            }

            var result = numbers.Sum();
            return result;
        }

        public static Type TypeFromPostgresType(string typeString)
        {
            var index = typeString.IndexOf("(", StringComparison.Ordinal);
            if (index != -1)
                typeString = typeString.Remove(index);

            if (PostgreTypeToType.ContainsKey(typeString))
                return PostgreTypeToType[typeString];

            throw new ArgumentException($"{typeString} is not a supported .NET class");
        }

    }
}
