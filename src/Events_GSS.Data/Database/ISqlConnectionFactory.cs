using Microsoft.Data.SqlClient;

namespace Events_GSS.Data.Database;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}