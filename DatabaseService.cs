using Dapper;
using Microsoft.Data.Sqlite;
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
            // A note about Message_Count
            // Neither Twitch nor keep track of the amount of messages a user has ever sent, at least not within their APIs
            // As such, the Message_Count Column will only interate starting from 0 from the moment this database is first created
            // It WILL NOT iterate when the bot is offline, so it will never be truly accurate
            // In the case of Twitch specifically, it wont even begin to count messages until the user follows the stream
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
                    GamesLost INTEGER DEFAULT 0,
                    Message_Count INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS CommandLog (
                    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID TEXT,
                    CommandName TEXT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                );";

            connection.Execute(tableBuildScript);




        }

        public record LeaderboardEntry(string Username, int GamesPlayed, int GamesWon, int GamesLost);

        public virtual IEnumerable<LeaderboardEntry> GetTopPlayers(int limit = 10)
        {
            using var db = GetConnection();
            
            string query = @"
        SELECT Username, GamesPlayed, GamesWon, GamesLost 
        FROM ServerMembers 
        WHERE GamesPlayed > 0
        ORDER BY GamesWon DESC, GamesPlayed ASC 
        LIMIT @Limit;";

            return db.Query<LeaderboardEntry>(query, new { Limit = limit });
        }
        public IDbConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={_dbPath}");
        }
    }
}

