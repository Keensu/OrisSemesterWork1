using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class JsonResult : IHttpResult
    {
        private readonly object _data;
        private readonly int _statusCode;

        public JsonResult(object data) : this(data, 200) { }

        public JsonResult(object data, int statusCode)
        {
            _data = data;
            _statusCode = statusCode;
        }

        public string Execute(HttpListenerContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = _statusCode; 

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(_data, options);
        }
    }
}
