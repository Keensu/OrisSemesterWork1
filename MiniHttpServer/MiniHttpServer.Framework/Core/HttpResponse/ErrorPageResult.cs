using MiniTemplateEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class ErrorPageResult : IHttpResult
    {
        private readonly int _statusCode;
        private readonly string _message;
        private readonly string _templatePath;

        public ErrorPageResult(int statusCode, string message, string templatePath = null)
        {
            _statusCode = statusCode;
            _message = message;
            _templatePath = templatePath ?? @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\error.thtml";
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = _statusCode;
            context.Response.ContentType = "text/html; charset=utf-8";


            if (File.Exists(_templatePath))
            {
                var renderer = new HtmlTemplateRenderer();
                return renderer.RenderFromFile(_templatePath, new { Message = _message, StatusCode = _statusCode });
            }

            return $@"
            <!DOCTYPE html>
            <html>
            <head><title>Ошибка {_statusCode}</title></head>
            <body>
                <h1>Ошибка {_statusCode}</h1>
                <p>{_message}</p>
                <a href='/'>Вернуться на главную</a>
            </body>
            </html>";
        }
    }
}
