using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Settings;
using MiniHttpServer.shared;
using System.Net;
using System.Reflection;

namespace MiniHttpServer.Tests
{
    [TestClass]
    public class HttpServerTests
    {
        private HttpServer _server;
        private static int _portCounter = 9000;

        private SettingsModel CreateTestSettings()
        {
            return new SettingsModel
            {
                Domain = "localhost",
                Port = GetNextPort(),
                PublicDirectoryPath = "public"
            };
        }

        private static int GetNextPort()
        {
            return _portCounter++;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ResetConfigSingleton();
            _server = new HttpServer(CreateTestSettings());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _server?.Stop();
        }

        private bool GetServerIsRunning(HttpServer server)
        {
            var isRunningField = typeof(HttpServer).GetField("_isRunning",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)isRunningField.GetValue(server);
        }

        [TestMethod]
        public void Constructor_WithValidSettings_InitializesConfig()
        {
            // Arrange
            var settings = new SettingsModel
            {
                Domain = "test",
                Port = GetNextPort(),
                PublicDirectoryPath = "wwwpath"
            };

            // Act
            var server = new HttpServer(settings);

            // Assert
            Assert.IsNotNull(server);

            var configInstance = typeof(Config).GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            Assert.IsNotNull(configInstance);

            server.Stop();
        }

        [TestMethod]
        public void Start_WhenCalled_SetsIsRunningToTrue()
        {
            // Act
            _server.Start();

            // Assert
            bool isRunning = GetServerIsRunning(_server);
            Assert.IsTrue(isRunning);
        }

        [TestMethod]
        public void Stop_WhenCalled_SetsIsRunningToFalse()
        {
            // Arrange
            _server.Start();
            bool wasRunning = GetServerIsRunning(_server);
            Assert.IsTrue(wasRunning);

            // Act
            _server.Stop();

            // Assert
            bool isRunningNow = GetServerIsRunning(_server);
            Assert.IsFalse(isRunningNow);
        }

        [TestMethod]
        public void HttpServer_StartStop_Sequence_WorksCorrectly()
        {
            // Act & Assert
            _server.Start();
            bool afterStart = GetServerIsRunning(_server);
            Assert.IsTrue(afterStart);

            _server.Stop();
            bool afterStop = GetServerIsRunning(_server);
            Assert.IsFalse(afterStop);
        }

        [TestMethod]
        public void Receive_WhenNotRunning_DoesNotBeginGetContext()
        {
            // Arrange
            _server.Stop();

            // Act
            var receiveMethod = typeof(HttpServer).GetMethod("Receive",
                BindingFlags.NonPublic | BindingFlags.Instance);
            receiveMethod?.Invoke(_server, null);

            // Assert
            bool isRunning = GetServerIsRunning(_server);
            Assert.IsFalse(isRunning);
        }

        [TestMethod]
        public void SettingsModel_Properties_SetAndGetCorrectly()
        {
            // Arrange & Act
            var settings = new SettingsModel
            {
                PublicDirectoryPath = "test_path",
                Domain = "test.domain",
                Port = 1122
            };

            // Assert
            Assert.AreEqual("test_path", settings.PublicDirectoryPath);
            Assert.AreEqual("test.domain", settings.Domain);
            Assert.AreEqual(1122, settings.Port);
        }

        [TestMethod]
        public void Config_SingletonPattern_ReturnsSameInstance()
        {
            // Arrange - сбрасываем Config
            ResetConfigSingleton();

            var settings1 = new SettingsModel { Domain = "test1", Port = GetNextPort() };
            var settings2 = new SettingsModel { Domain = "test2", Port = GetNextPort() };

            // Act
            Config.Initialize(settings1);
            var instance1 = Config.Instance;

            Config.Initialize(settings2);
            var instance2 = Config.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
            Assert.AreEqual("test1", instance1.Settings.Domain);
        }

        [TestMethod]
        public void Config_Instance_WithoutInitialize_ReturnsDefaultInstance()
        {
            // Arrange - сбрасываем Config
            ResetConfigSingleton();

            // Act
            var instance = Config.Instance;

            // Assert
            Assert.IsNotNull(instance);
            Assert.IsNotNull(instance.Settings);

            ResetConfigSingleton();
        }

        [TestMethod]
        public void Config_Initialize_AfterFirstCall_DoesNotChangeSettings()
        {
            // Arrange - сбрасываем Config
            ResetConfigSingleton();

            var settings1 = new SettingsModel { Domain = "first", Port = GetNextPort() };
            var settings2 = new SettingsModel { Domain = "second", Port = GetNextPort() };

            // Act
            Config.Initialize(settings1);
            var instance1 = Config.Instance;

            Config.Initialize(settings2);
            var instance2 = Config.Instance;

            // Assert
            Assert.AreEqual("first", instance1.Settings.Domain);
            Assert.AreEqual("first", instance2.Settings.Domain);
            Assert.AreEqual(settings1.Port, instance1.Settings.Port);
            Assert.AreEqual(settings1.Port, instance2.Settings.Port);

            ResetConfigSingleton();
        }

        [TestMethod]
        public void HttpServer_Stop_WhenNotRunning_DoesNotThrow()
        {
            // Arrange - сервер не запущен

            // Act & Assert - не должно быть исключения
            _server.Stop();

            bool isRunning = GetServerIsRunning(_server);
            Assert.IsFalse(isRunning);
        }

        [TestMethod]
        public void HttpServer_CanRestartAfterStop()
        {
            // Arrange
            _server.Start();
            _server.Stop();

            // Создаем новый сервер с новым портом для перезапуска
            var newServer = new HttpServer(CreateTestSettings());

            // Act
            newServer.Start();

            // Assert
            bool isRunning = GetServerIsRunning(newServer);
            Assert.IsTrue(isRunning);

            newServer.Stop();
        }

        private void ResetConfigSingleton()
        {
            var instanceField = typeof(Config).GetField("_instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);
        }
    }
}