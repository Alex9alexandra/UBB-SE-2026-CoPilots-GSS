using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Database;

public class SqlConnectionFactory
{
    private readonly string _connectionString;
     public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
