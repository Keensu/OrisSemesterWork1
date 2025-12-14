using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Models;
using Npgsql;
using System.Collections.Generic;
using System.Linq;

namespace MyORMLibraryTests
{
    [TestClass]
    [DoNotParallelize]
    public class ORMContextTests
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Database=users;Username=postgres;Password=g7-gh-c5hc;";
        private ORMContext? _context;

        [TestInitialize]
        public void Setup()
        {
            _context = new ORMContext(ConnectionString);

            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                DROP TABLE IF EXISTS ""users"";
                CREATE TABLE IF NOT EXISTS ""users"" (
                    ""id"" SERIAL PRIMARY KEY,
                    ""user_name"" VARCHAR(50),
                    ""email"" VARCHAR(50),
                    ""password"" VARCHAR(50),
                    ""phone"" VARCHAR(20),
                    ""role"" VARCHAR(20),
                    ""date_registered"" TIMESTAMP WITHOUT TIME ZONE
                );
                TRUNCATE TABLE ""users"" RESTART IDENTITY;", conn);
            cmd.ExecuteNonQuery();
        }

        [TestCleanup]
        public void Cleanup()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"TRUNCATE TABLE ""users"" RESTART IDENTITY;", conn);
            cmd.ExecuteNonQuery();
        }

        [TestMethod]
        public void WhenCreatingNewUser_ItShouldBeStoredInDatabase()
        {
            var user = new User { UserName = "adam01", Email = "adam@example.com", Password = "passA1" };
            _context.Create(user);

            var users = _context.ReadByAll<User>().ToList();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual("adam01", users[0].UserName);
            Assert.AreEqual("adam@example.com", users[0].Email);
            Assert.AreEqual("passA1", users[0].Password);
        }

        [TestMethod]
        public void WhenReadingUserById_ItShouldReturnCorrectRecord()
        {
            var user = new User { UserName = "emma_l", Email = "emma@example.com", Password = "ePass!" };
            _context.Create(user);

            int id = _context.ReadByAll<User>().First().Id;
            var result = _context.ReadById<User>(id);

            Assert.IsNotNull(result);
            Assert.AreEqual("emma_l", result.UserName);
            Assert.AreEqual("emma@example.com", result.Email);
            Assert.AreEqual("ePass!", result.Password);
        }

        [TestMethod]
        public void WhenUpdatingExistingUser_ItShouldApplyChanges()
        {
            var user = new User { UserName = "liamX", Email = "liam@mail.com", Password = "liam123" };
            _context.Create(user);

            int id = _context.ReadByAll<User>().First().Id;

            user.UserName = "liamUpdated";
            user.Email = "liam.updated@mail.com";
            user.Password = "newLiamPass";

            _context.Update(id, user);

            var updated = _context.ReadById<User>(id);
            Assert.AreEqual("liamUpdated", updated.UserName);
            Assert.AreEqual("liam.updated@mail.com", updated.Email);
            Assert.AreEqual("newLiamPass", updated.Password);
        }

        [TestMethod]
        public void WhenDeletingUser_ItShouldBeRemovedFromDatabase()
        {
            var user = new User { UserName = "sof", Email = "sofia@mail.com", Password = "sofPass" };
            _context.Create(user);

            int id = _context.ReadByAll<User>().First().Id;
            _context.Delete<User>(id);

            Assert.AreEqual(0, _context.ReadByAll<User>().Count);
        }

        [TestMethod]
        public void WhenReadingAllUsers_ItShouldReturnAllRecords()
        {
            _context.Create(new User { UserName = "noahN", Email = "noah@mail.com", Password = "n1" });
            _context.Create(new User { UserName = "oliv", Email = "olivia@mail.com", Password = "o2" });

            var users = _context.ReadByAll<User>().ToList();
            Assert.AreEqual(2, users.Count);
        }

        [TestMethod]
        public void WhenFilteringByUserName_ShouldReturnCorrectUsers()
        {
            _context.Create(new User { UserName = "avaA", Email = "ava@mail.com", Password = "a1" });
            _context.Create(new User { UserName = "lucL", Email = "lucas@mail.com", Password = "l1" });
            _context.Create(new User { UserName = "ava2", Email = "ava2@mail.com", Password = "a2" });

            var users = _context.Where<User>(u => u.UserName == "avaA").ToList();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual("avaA", users[0].UserName);
        }

        [TestMethod]
        public void WhenGettingFirstMatchingRecordByUserName_ShouldReturnCorrectUser()
        {
            _context.Create(new User { UserName = "han1", Email = "h1@mail.com", Password = "h1" });
            _context.Create(new User { UserName = "han2", Email = "h2@mail.com", Password = "h2" });

            var user = _context.FirstOrDefault<User>(u => u.UserName == "han1");
            Assert.IsNotNull(user);
            Assert.AreEqual("han1", user.UserName);
        }
    }
}
