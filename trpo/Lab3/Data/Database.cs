// Уровень доступа к данным, инфраструктура: управляет соединением SQLite,
// создает таблицу и выполняет SQL-команды. Про смысл записей ничего не знает -
// запросы к таблице Sales формирует SaleRepository.

using Microsoft.Data.Sqlite;

namespace Lab3.Data;

public static class Database
{
    // база хранится рядом с программой
    private static readonly string ConnectionString =
        "Data Source=" + Path.Combine(AppContext.BaseDirectory, "sales.db");

    private static SqliteConnection? _connection;

    // Открывает соединение и при первом запуске создает таблицу.
    public static void Open()
    {
        _connection = new SqliteConnection(ConnectionString);
        _connection.Open();

        Execute("""
            CREATE TABLE IF NOT EXISTS Sales (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Shop     INTEGER NOT NULL,
                Section  INTEGER NOT NULL,
                Receipt  INTEGER NOT NULL,
                Product  TEXT    NOT NULL,
                Article  TEXT    NOT NULL,
                Price    REAL    NOT NULL,
                Quantity INTEGER NOT NULL,
                SaleDate TEXT    NOT NULL
            )
            """);
    }

    // Выполняет команду без результата (INSERT, UPDATE, DELETE, CREATE).
    public static void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        command.ExecuteNonQuery();
    }

    // Выполняет запрос и читает его результат через функцию map.
    public static List<T> Query<T>(string sql, Func<SqliteDataReader, T> map,
        params (string Name, object Value)[] parameters)
    {
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);

        var result = new List<T>();

        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(map(reader));

        return result;
    }
}
