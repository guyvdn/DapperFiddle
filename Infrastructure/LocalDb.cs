using FluentMigrator.Runner;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DapperFiddle.Infrastructure
{
    public static class LocalDb
    {
        private const string DatabaseName = "dapper";
        private const string MasterConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";
        public const string ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=dapper;Integrated Security=True";

        public static void Create()
        {
            var databaseFileName = Path.Combine(AppContext.BaseDirectory, DatabaseName + ".mdf");

            ExecuteCommand($"CREATE DATABASE {DatabaseName} ON (NAME = N'{DatabaseName}', FILENAME = '{databaseFileName}')");
        }

        public static void Drop()
        {
            ExecuteCommand($"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;" +
                           $"DROP DATABASE [{DatabaseName}]");
        }

        public static void Stop()
        {
            // Don't leave LocalDB process running (fix test runner warning)
            using var process = Process.Start("sqllocaldb", "stop MSSQLLocalDB");
            process?.WaitForExit();
        }

        private static void ExecuteCommand(string commandText)
        {
            using var connection = new SqlConnection(MasterConnectionString);
            connection.Open();
            using var command = new SqlCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        public static void Migrate()
        {
            var provider = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(runnerBuilder => runnerBuilder
                    .AddSqlServer()
                    .WithGlobalConnectionString(ConnectionString)
                    .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
                .AddLogging(loggingBuilder => loggingBuilder.AddFluentMigratorConsole())
                .BuildServiceProvider();

            var runner = provider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        }
    }
}