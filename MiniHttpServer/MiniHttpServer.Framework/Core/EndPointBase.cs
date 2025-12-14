using MiniHttpServer.Framework.Core.HttpResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core
{
    public abstract class EndpointBase
    {
        protected HttpListenerContext Context { get; private set; }

        internal void SetContext(HttpListenerContext context)
        {
            Context = context;
        }

        protected IHttpResult Page(string pathTemplate, object data) => new PageResult(pathTemplate, data);

        protected IHttpResult Json(object data) => new JsonResult(data);

        protected int? GetUserIdFromCookie()
        {
            var cookies = Context.Request.Cookies;
            if (cookies == null)
                return null;

            foreach (Cookie cookie in cookies) 
            {
                if (cookie.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(cookie.Value, out int userId) && userId > 0)
                    {
                        return userId;
                    }
                }
            }

            return null;
        }
    }

}
