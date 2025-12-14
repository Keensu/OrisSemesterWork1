using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.shared;
using System.Net;
using System.Text.Json;
using BCrypt.Net;
using MiniHttpServer.Models;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class UsersEndPoint : EndpointBase
    {
        private readonly string _connectionString;
        public UsersEndPoint()
        {
            _connectionString = "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=g7-gh-c5hc;";
        }

        [HttpGet("users/get")]
        public IHttpResult GetUsers()
        {
            ORMContext context = new ORMContext(_connectionString);
            var users = context.ReadByAll<User>();

            var safeUsers = users.Select(u => new
            {
                u.Id,
                u.Email,
                u.Phone,
                u.UserName,
                u.Role,
                u.DateRegistered,
            });

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\users.thtml", new { Users = safeUsers });
        }

        [HttpGet("users/get/{id}")]
        public IHttpResult GetUserById(int id)
        {
            ORMContext context = new ORMContext(_connectionString);
            var user = context.ReadById<User>(id);

            if (user == null)
                return new NotFoundResult($"User with id {id} not found");

            var safeUser = new
            {
                user.Id,
                user.Email,
                user.Phone,
                user.UserName,
                user.Role,
                user.DateRegistered,
            };

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\users.thtml", new { User = safeUser });
        }

        [HttpGet("users/get/email/{email}")]
        public IHttpResult GetUserByEmail(string email)
        {
            ORMContext context = new ORMContext(_connectionString);
            var user = context.FirstOrDefault<User>(u => u.Email == email);

            if (user == null)
                return new NotFoundResult($"User with email {email} not found");

            var safeUser = new
            {
                user.Id,
                user.Email,
                user.Phone,
                user.UserName,
                user.Role,
                user.DateRegistered,
            };

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\users.thtml", new { User = safeUser });
        }

        [HttpDelete("users/delete/{id}")]
        public IHttpResult DeleteUsers(int id)
        {
            ORMContext context = new ORMContext(_connectionString);

            var existingUser = context.ReadById<User>(id);
            if (existingUser == null)
            {
                return new NotFoundResult($"User with id {id} not found");
            }

            context.Delete<User>(id);

            return new JsonResult(new { success = true, message = "User deleted successfully" });
        }

        [HttpPut("users/update/{id}")]
        public IHttpResult UpdateUsers(int id, User user)
        {
            ORMContext context = new ORMContext(_connectionString);

            var existingUser = context.ReadById<User>(id);
            if (existingUser == null)
            {
                return new NotFoundResult($"User with id {id} not found");
            }

            if (!string.IsNullOrEmpty(user.Email) && user.Email != existingUser.Email)
            {
                var userWithSameEmail = context.FirstOrDefault<User>(u => u.Email == user.Email);
                if (userWithSameEmail != null && userWithSameEmail.Id != id)
                {
                    return new ConflictResult("User with this email already exists");
                }
            }

            existingUser.Email = user.Email ?? existingUser.Email;
            existingUser.Phone = user.Phone ?? existingUser.Phone;
            existingUser.UserName = user.UserName ?? existingUser.UserName;
            existingUser.Role = user.Role ?? existingUser.Role;

            context.Update(id, existingUser);

            return new JsonResult(new { success = true, message = "User updated successfully" });
        }

        [HttpPut("users/update/{id}/password")]
        public IHttpResult UpdateUserPassword(int id, PasswordUpdateRequest request)
        {
            var context = new ORMContext(_connectionString);
            var user = context.ReadById<User>(id);
            if (user == null)
            {
                return new NotFoundResult($"User with id {id} not found");
            }

            // Хэшируем новый пароль
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            context.Update(id, user);

            return new JsonResult(new { success = true, message = "Password updated successfully" });
        }

        [HttpPost("users/register")]
        public IHttpResult PostUsers(User user)
        {
            if (string.IsNullOrEmpty(user.Email) ||
                string.IsNullOrEmpty(user.UserName) ||
                string.IsNullOrEmpty(user.Password))
            {
                return new BadRequestResult("Email, Username and Password are required");
            }

            var context = new ORMContext(_connectionString);

            if (context.FirstOrDefault<User>(u => u.Email == user.Email) != null)
                return new ConflictResult("User with this email already exists");

            if (context.FirstOrDefault<User>(u => u.UserName == user.UserName) != null)
                return new ConflictResult("Username is already taken");

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.Role = user.Role ?? "user";
            user.DateRegistered = DateTime.Now;

            context.Create(user);

            var cookieValue = $"UserId={user.Id}; Path=/; HttpOnly";
            Context.Response.Headers.Add("Set-Cookie", cookieValue);
            return new RedirectResult("/home");
        }

        [HttpPost("users/authenticate")]
        public IHttpResult AuthenticateUser(string UserName, string Password)
        {
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
            {
                return new BadRequestResult("Username and password are required");
            }

        var context = new ORMContext(_connectionString);
            var user = context.FirstOrDefault<User>(u => u.UserName == UserName);

            // Проверка хэша
            if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                return new UnauthorizedResult("Invalid username or password");
            }

            var cookieValue = $"UserId={user.Id}; Path=/; HttpOnly";
            Context.Response.Headers.Add("Set-Cookie", cookieValue);
            return new RedirectResult("/home");
        }

        [HttpGet("users/search")]
        public IHttpResult SearchUsers(string email = null, string userName = null, string role = null)
        {
            ORMContext context = new ORMContext(_connectionString);

            var query = context.ReadByAll<User>().AsQueryable();

            if (!string.IsNullOrEmpty(email))
                query = query.Where(u => u.Email.Contains(email));

            if (!string.IsNullOrEmpty(userName))
                query = query.Where(u => u.UserName.Contains(userName));

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            var users = query.ToList();

            var safeUsers = users.Select(u => new
            {
                u.Id,
                u.Email,
                u.Phone,
                u.UserName,
                u.Role,
                u.DateRegistered,
            });

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\users.thtml", new { Users = safeUsers });
        }
    }

    public class PasswordUpdateRequest
    {
        public string NewPassword { get; set; }
    }
}

