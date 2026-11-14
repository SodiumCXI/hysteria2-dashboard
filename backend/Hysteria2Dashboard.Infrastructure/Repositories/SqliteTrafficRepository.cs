using Hysteria2Dashboard.Application.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Hysteria2Dashboard.Infrastructure.Repositories;

public class SqliteTrafficRepository : ITrafficRepository
{
    private readonly string _connectionString;

    public SqliteTrafficRepository(IConfiguration configuration)
    {
        var path = Path.GetFullPath(configuration["App:DatabasePath"] ?? "/app/data/traffic.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();

        using var connection = Open();
        Execute(connection, "PRAGMA journal_mode = WAL");
        Execute(connection,
            "CREATE TABLE IF NOT EXISTS traffic (" +
            "username TEXT PRIMARY KEY, tx INTEGER NOT NULL, rx INTEGER NOT NULL) WITHOUT ROWID");
    }

    public Task<Dictionary<string, (long TxBytes, long RxBytes)>> GetAllAsync()
    {
        var traffic = new Dictionary<string, (long TxBytes, long RxBytes)>();

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT username, tx, rx FROM traffic ORDER BY username";

        using var reader = command.ExecuteReader();
        while (reader.Read())
            traffic[reader.GetString(0)] = (reader.GetInt64(1), reader.GetInt64(2));

        return Task.FromResult(traffic);
    }

    public Task AddAsync(Dictionary<string, (long TxBytes, long RxBytes)> traffic)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT INTO traffic (username, tx, rx) VALUES ($username, $tx, $rx) " +
            "ON CONFLICT (username) DO UPDATE SET tx = tx + excluded.tx, rx = rx + excluded.rx";

        var username = command.Parameters.Add("$username", SqliteType.Text);
        var tx = command.Parameters.Add("$tx", SqliteType.Integer);
        var rx = command.Parameters.Add("$rx", SqliteType.Integer);

        foreach (var (name, bytes) in traffic)
        {
            username.Value = name.ToLowerInvariant();
            tx.Value = bytes.TxBytes;
            rx.Value = bytes.RxBytes;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string username)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM traffic WHERE username = $username";
        command.Parameters.AddWithValue("$username", username.ToLowerInvariant());
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
