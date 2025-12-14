using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class NoContenResult : IHttpResult
    {
        public string Execute(HttpListenerContext context)
        {
            context.Response.StatusCode = 204;
            return string.Empty;
        }
    }
}
