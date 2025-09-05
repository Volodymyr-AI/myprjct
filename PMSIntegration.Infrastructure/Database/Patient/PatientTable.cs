using System.Data.SQLite;

namespace PMSIntegration.Infrastructure.Database.Patient;

public static class PatientTable
{
    public static void CreatePatientTable(SQLiteConnection connection)
    {
        string createPatientTableSql = """
                                           CREATE TABLE IF NOT EXISTS Patients (
                                               Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                               FirstName TEXT NOT NULL,
                                               LastName TEXT NOT NULL,
                                               Phone TEXT NOT NULL,
                                               Email TEXT,
                                               Address TEXT NOT NULL,
                                               City TEXT NOT NULL,
                                               [State] TEXT NOT NULL,
                                               ZipCode TEXT NOT NULL,
                                               DataExported BOOLEAN NOT NULL,
                                               BirthDate TEXT
                                           );
                                       """;
        
        using var command = new SQLiteCommand(createPatientTableSql, connection);
        command.ExecuteNonQuery();
    }
}