using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Data;

namespace RemnaBotService
{
    public class DatabaseService
    {
        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _dbPath = Path.Combine(baseDir, "Data/SlytaBot.db");

        public DatabaseService()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string directory = Path.GetDirectoryName(_dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection($"Data Source={_dbPath}");

            string tableBuildScript = @"
                CREATE TABLE IF NOT EXISTS Viewers (
                    UserID TEXT PRIMARY KEY,
                    Username TEXT NOT NULL,
                    FollowDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsModerator INTEGER DEFAULT 0,
                    Message_Count INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS ServerMembers (
                    UserID TEXT PRIMARY KEY,
                    Username TEXT NOT NULL,
                    JoinDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsModerator INTEGER DEFAULT 0,
                    GamesPlayed INTEGER DEFAULT 0,
                    GamesWon INTEGER DEFAULT 0,
                    GamesLost INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS CommandLog (
                    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID TEXT,
                    CommandName TEXT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                );";

            connection.Execute(tableBuildScript);

        


        }

        public IDbConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={_dbPath}");
        }
    }
}

