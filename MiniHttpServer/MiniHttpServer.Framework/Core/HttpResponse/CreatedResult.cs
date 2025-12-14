using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class CreatedResult : IHttpResult

    {
        private readonly string _location;
        private readonly object _data;

        public CreatedResult(string location, object data)
        {
            _location = location;
            _data = data;
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = 201;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Location", _location);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(_data, options);
        }
    }
}
