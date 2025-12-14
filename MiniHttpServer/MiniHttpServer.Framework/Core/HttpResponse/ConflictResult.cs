using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class ConflictResult : IHttpResult
    {
        private readonly string _message;

        public ConflictResult(string message = "Conflict")
        {
            _message = message;
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = 409;
            context.Response.ContentType = "application/json";

            return JsonSerializer.Serialize(new { error = _message });
        }
    }
}
