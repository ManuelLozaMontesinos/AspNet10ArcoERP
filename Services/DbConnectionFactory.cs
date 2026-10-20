using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AspNet10ArcoERP.Services
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private static string? _workingConnectionString;
        private static readonly object _lock = new();

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            if (!string.IsNullOrEmpty(_workingConnectionString))
            {
                var conn = new SqlConnection(_workingConnectionString);
                conn.Open();
                return conn;
            }

            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_workingConnectionString))
                {
                    var conn = new SqlConnection(_workingConnectionString);
                    conn.Open();
                    return conn;
                }

                string defaultCs = _configuration.GetConnectionString("DefaultConnection") 
                    ?? "Server=.\\SQLEXPRESS;Database=esderp_v6;Trusted_Connection=True;TrustServerCertificate=True;";
                string fallbackCs = _configuration.GetConnectionString("FallbackConnection") 
                    ?? "Server=localhost;Database=esderp_v6;User Id=sa;Password=admin123*;TrustServerCertificate=True;";

                string[] candidates = new[] { defaultCs, fallbackCs };

                foreach (var cs in candidates)
                {
                    try
                    {
                        var conn = new SqlConnection(cs);
                        conn.Open();
                        _workingConnectionString = cs;
                        return conn;
                    }
                    catch
                    {
                        // try next
                    }
                }

                _workingConnectionString = defaultCs;
                var fallbackConn = new SqlConnection(_workingConnectionString);
                fallbackConn.Open();
                return fallbackConn;
            }
        }
    }
}
