using AwesomeAssertions;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Nop.Data.Migrations.UpgradeTo500;
using NUnit.Framework;

namespace Nop.Tests.Nop.Data.Tests;

[TestFixture]
public class SearchTermMigrationTests : BaseNopTest
{
    [Test]
    public void UpEmptiesSearchTermsBeforeAddingRequiredColumns()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"search-term-migration-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=False";

        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE Customer (Id INTEGER NOT NULL CONSTRAINT PK_Customer PRIMARY KEY);
                    CREATE TABLE SearchTerm (
                        Id INTEGER NOT NULL CONSTRAINT PK_SearchTerm PRIMARY KEY,
                        Keyword TEXT NULL,
                        StoreId INTEGER NOT NULL,
                        Count INTEGER NOT NULL
                    );
                    INSERT INTO SearchTerm (Id, Keyword, StoreId, Count) VALUES (1, 'shoes', 1, 3);
                    """;
                command.ExecuteNonQuery();
            }

            var services = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(builder => builder
                    .AddSQLite(compatibilityMode: CompatibilityMode.LOOSE)
                    .WithGlobalConnectionString(connectionString))
                .BuildServiceProvider();

            using (services)
                services.GetRequiredService<IMigrationRunner>().Up(new SearchTermMigration());

            using var verify = new SqliteConnection(connectionString);
            verify.Open();
            using var countCommand = verify.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(*) FROM SearchTerm;";
            Convert.ToInt64(countCommand.ExecuteScalar()).Should().Be(0);

            using var columnCommand = verify.CreateCommand();
            columnCommand.CommandText = "SELECT \"notnull\" FROM pragma_table_info('SearchTerm') WHERE name = 'CreatedOnUtc';";
            Convert.ToInt64(columnCommand.ExecuteScalar()).Should().Be(1, "CreatedOnUtc must be added as NOT NULL only after the table is empty");
        }
        finally
        {
            TryDelete(dbPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // The runner may still be releasing the file.
        }
    }
}
