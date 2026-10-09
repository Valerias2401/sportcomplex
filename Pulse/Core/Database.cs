using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.Core
{
    internal class Database
    {
        private static string connectionString =
        "Host=localhost;Port=5432;Database=Pulse;Username=postgres;Password=love";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
