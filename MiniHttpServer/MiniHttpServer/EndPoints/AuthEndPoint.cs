using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Services;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.HttpResponse;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class AuthEndpoint : EndpointBase
    {
        private readonly EmailService emailService = new();

        [HttpPost ("auth")]
        public async Task Login(string email, string password) 
        {
            await emailService.SendEmailAsync(email, "Успешная авторизация", password);
        }
    }
}
