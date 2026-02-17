using Microsoft.Data.SqlClient;

namespace CommunityOS.Api.Services;

public static class DbProviderSelector
{
    public static bool CanConnectToSqlServer(string connectionString)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn.State == System.Data.ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }
}
