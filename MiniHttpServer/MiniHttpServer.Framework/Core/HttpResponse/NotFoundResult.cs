using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class NotFoundResult : IHttpResult
    {
        private readonly string _message;

        public NotFoundResult(string message = "Not Found")
        {
            _message = message;
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";

            return JsonSerializer.Serialize(new { error = _message });
        }
    }
}
