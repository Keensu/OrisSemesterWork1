using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Handlers;
using MiniHttpServer.FrameWork.Core.Handlers;
using MiniHttpServer.Settings;
using System.Net;
using System.Net.Mime;

namespace MiniHttpServer.shared
{
    public class HttpServer
    {
        private HttpListener _listener;
        private SettingsModel _config;
        private bool _isRunning = false;

        public HttpServer(SettingsModel config)
        {
            _config = config;
            Config.Initialize(config);
            _listener = new HttpListener();
        }

        public void Start()
        {
            if (_isRunning)
            {
                Console.WriteLine("Сервер уже запущен");
                return;
            }

            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://{_config.Domain}:{_config.Port}/");
            _listener.Start();
            _isRunning = true;
            Console.WriteLine("Сервер запущен");
            Console.WriteLine("Введите в адресную строку:");
            Console.WriteLine($"http://{_config.Domain}:{_config.Port}/Tours/MainPage.html");
            Receive();
        }

        public void Stop()
        {
            if (!_isRunning) { return; }

            _isRunning = false;


            _listener.Stop();
            Console.WriteLine("Сервер остановил работу");
        }

        private void Receive()
        {
            if (_isRunning)
            {
                try
                {
                    _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async void ListenerCallback(IAsyncResult result)
        {
            if (!_listener.IsListening)
                return;

            try
            {
                var context = _listener.EndGetContext(result);

                Handler staticFilesHandler = new StaticFilesHandler();
                Handler endPointsHandler = new EndPointsHandler(); 
                staticFilesHandler.Successor = endPointsHandler;

                staticFilesHandler.HandleRequest(context);

                Console.WriteLine("Запрос обработан");
                Receive();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ListenerCallback: {ex.Message}");
                if (_isRunning) Receive();
            }
        }
    }
}