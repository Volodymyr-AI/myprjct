using System.Data.SQLite;

namespace PMSIntegration.Infrastructure.Database.Config;

public static class ConfigTable
{
    public static void CreateConfigTable(SQLiteConnection connection)
    {
        string createSql = """
                               CREATE TABLE IF NOT EXISTS Config (
                                   Key TEXT PRIMARY KEY,
                                   Value TEXT NOT NULL
                               );
                           """;

        using var command = new SQLiteCommand(createSql, connection);
        command.ExecuteNonQuery();
    }

    public static void InsertOrUpdate(SQLiteConnection connection, string key, string value)
    {
        string insertSql = """
                               INSERT INTO Config (Key, Value) VALUES (@key, @value)
                               ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
                           """;

        using var command = new SQLiteCommand(insertSql, connection);
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        command.ExecuteNonQuery();
    }

    public static bool IsEmpty(SQLiteConnection connection)
    {
        string countSql = "SELECT COUNT(*) FROM Config";
        using var command = new SQLiteCommand(countSql, connection);
        var count = Convert.ToInt32(command.ExecuteScalar());
        return count == 0;
    }
}