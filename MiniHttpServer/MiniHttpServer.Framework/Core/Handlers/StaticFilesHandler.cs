using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Settings;
using MiniHttpServer.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Handlers
{
    class StaticFilesHandler : Handler
    {
        public override async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var isGetMethod = request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
            var pathParts = request.Url.AbsolutePath.Split('/');
            var isStaticFile = pathParts.Any(x => x.Contains("."));

            if (isGetMethod && isStaticFile)
            {
                var response = context.Response;
                byte[]? responseFile = null;
                bool fileFound = false;

                try
                {
                    string filePath = Path.Combine(Config.Instance.Settings.PublicDirectoryPath, request.Url.AbsolutePath.TrimStart('/'));

                    if (request.Url.AbsolutePath.EndsWith("/"))
                    {
                        filePath = Path.Combine(filePath, "index.html");
                    }

                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"File not found: {filePath}");
                    }

                    responseFile = await File.ReadAllBytesAsync(filePath);
                    response.ContentType = GetContentType.Invoke(filePath);
                    response.ContentLength64 = responseFile.Length;
                    fileFound = true;
                }
                catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                {
                    Console.WriteLine("Файл или директория не найдена: " + ex.Message);
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.ContentType = "text/plain";
                    responseFile = Encoding.UTF8.GetBytes("404 Not Found");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при обработке статического файла: " + ex.Message);
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.ContentType = "text/plain";
                    responseFile = Encoding.UTF8.GetBytes("500 Internal Server Error");
                }

                response.ContentLength64 = responseFile.Length;
                await response.OutputStream.WriteAsync(responseFile, 0, responseFile.Length);
                response.Close();
            }
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }
    }
}
