using MiniHttpServer.Framework.Core.HttpResponse;
using System;
using System.Net;

public class RedirectResult : IHttpResult
{
    private readonly string _location;

    public RedirectResult(string location)
    {
        _location = location ?? throw new ArgumentNullException(nameof(location));
    }

    public string Execute(HttpListenerContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Found; // 302
        context.Response.Headers["Location"] = _location;
        context.Response.ContentLength64 = 0;

        return string.Empty; 
    }
}