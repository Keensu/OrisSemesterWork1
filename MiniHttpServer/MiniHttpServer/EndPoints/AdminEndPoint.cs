using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Models;
using System;
using System.Linq;
using System.Text;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class AdminEndPoint : EndpointBase
    {
        private readonly string _connectionString;
        public AdminEndPoint()
        {
            _connectionString = "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=g7-gh-c5hc;";
        }

        private IHttpResult? EnsureAdmin()
        {
            var userId = GetUserIdFromCookie();
            if (userId == null)
                return new ErrorPageResult(401, "Требуется вход в систему");

            var context = new ORMContext(_connectionString);
            var user = context.ReadById<User>(userId.Value);

            if (user == null || user.Role != "admin")
                return new ErrorPageResult(403, "Требуются права администратора");

            return null;
        }


        [HttpGet("admin")]
        public IHttpResult Index()
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\index.thtml",
                null
            );
        }


        [HttpGet("admin/hotels")]
        public IHttpResult Hotels()
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            var context = new ORMContext(_connectionString);
            var hotels = context.ReadByAll<Hotel>().OrderBy(h => h.Id).ToList();

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\hotels.thtml",
                new { Hotels = hotels }
            );
        }

        [HttpGet("admin/hotels/create")]
        public IHttpResult CreateHotelForm()
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\hotel-create.thtml",
                null
            );
        }

        [HttpPost("admin/hotels/create")]
        public IHttpResult CreateHotel(Hotel hotel)
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            // Валидация
            if (string.IsNullOrWhiteSpace(hotel.Name))
                return new BadRequestResult("Название отеля обязательно");

            var context = new ORMContext(_connectionString);
            context.Create(hotel);
            return new RedirectResult("/admin/hotels");
        }

        [HttpGet("admin/hotels/edit")]
        public IHttpResult EditHotelForm()
        {
            var idStr = Context.Request.QueryString["id"];
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id) || id <= 0)
                return new BadRequestResult("Требуется корректный параметр id");

            var authError = EnsureAdmin();
            if (authError != null) return authError;

            var context = new ORMContext(_connectionString);
            var hotel = context.ReadById<Hotel>(id);
            if (hotel == null)
                return new NotFoundResult("Отель не найден");

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\hotel-edit.thtml",
                new {
                Hotel = hotel,
                StarOptions = Enumerable.Range(1, 5).ToList() // [1,2,3,4,5]
            }
            );
        }

        [HttpPost("admin/hotels/edit")]
        public IHttpResult UpdateHotel(
        int Id,
        string Name,
        string? Description,
        string Address,
        string City,
        string Country,
        string? Phone,
        string? Email,
        int Stars)
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            if (Id <= 0)
                return new BadRequestResult("Неверный ID отеля");

            if (string.IsNullOrWhiteSpace(Name))
                return new BadRequestResult("Название отеля обязательно");

            if (Stars < 1 || Stars > 5)
                return new BadRequestResult("Количество звёзд должно быть от 1 до 5");

            var hotel = new Hotel
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Address = Address,
                City = City,
                Country = Country,
                Phone = Phone,
                Email = Email,
                Stars = Stars
            };

            try
            {
                var context = new ORMContext(_connectionString);
                context.Update(hotel.Id, hotel);
                return new RedirectResult("/admin/hotels");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Update failed: {ex}");
                return new BadRequestResult("Ошибка: " + ex.Message);
            }
        }

        [HttpPost("admin/hotels/delete")]
        public IHttpResult DeleteHotel(int id)
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            try
            {
                var context = new ORMContext(_connectionString);


                var existingRoom = context.FirstOrDefault<Room>(r => r.HotelId == id);

                if (existingRoom != null)
                {
                    return new JsonResult(new
                    {
                        error = $"Нельзя удалить отель: найден номер ID {existingRoom.Id}. Удалите все номера сначала."
                    }, 400);
                }

                context.Delete<Hotel>(id);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    error = "Ошибка удаления: " + ex.Message
                }, 500);
            }
        }



        [HttpGet("admin/rooms")]
        public IHttpResult Rooms()
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            var context = new ORMContext(_connectionString);
            var rooms = context.ReadByAll<Room>().OrderBy(r => r.Id).ToList();
            var hotels = context.ReadByAll<Hotel>().ToDictionary(h => h.Id, h => h.Name);

            var roomsWithHotel = rooms.Select(r => new
            {
                r.Id,
                r.HotelId,
                HotelName = hotels.GetValueOrDefault(r.HotelId, "Unknown"),
                r.RoomType,
                r.PricePerNight,
                r.Capacity,
                r.IsAvailable
            });

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\rooms.thtml",
                new { Rooms = roomsWithHotel }
            );
        }

        [HttpGet("admin/rooms/create")]
        public IHttpResult CreateRoomForm()
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            var context = new ORMContext(_connectionString);
            var hotels = context.ReadByAll<Hotel>();

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\room-create.thtml",
                new { Hotels = hotels }
            );
        }

        [HttpPost("admin/rooms/create")]
        public IHttpResult CreateRoom(
    int HotelId,
    string RoomType,
    decimal PricePerNight,
    int Capacity,
    string Area,
    string Amenities,
    string BedType,
    bool IsAvailable)
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            if (HotelId <= 0)
                return new JsonResult(new { error = "Не выбран отель." }, 400);

            if (string.IsNullOrWhiteSpace(RoomType))
                return new JsonResult(new { error = "Тип номера обязателен." }, 400);

            if (PricePerNight < 0)
                return new JsonResult(new { error = "Цена не может быть отрицательной." }, 400);

            if (Capacity <= 0)
                return new JsonResult(new { error = "Вместимость должна быть больше нуля." }, 400);

            try
            {
                var room = new Room
                {
                    HotelId = HotelId,
                    RoomType = RoomType.Trim(),
                    PricePerNight = PricePerNight,
                    Capacity = Capacity,
                    Area = Area,
                    Amenities = Amenities,
                    BedType = BedType,
                    IsAvailable = IsAvailable

                };

                var context = new ORMContext(_connectionString);
                context.Create(room);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Room creation failed: {ex}");
                return new JsonResult(new { error = "Ошибка при создании номера: " + ex.Message }, 500);
            }
        }

        [HttpGet("admin/rooms/edit")]
        public IHttpResult EditRoomForm()
        {
            var idStr = Context.Request.QueryString["id"];
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id) || id <= 0)
                return new BadRequestResult("Неверный ID номера");

            var authError = EnsureAdmin();
            if (authError != null) return authError;

            var context = new ORMContext(_connectionString);
            var room = context.ReadById<Room>(id);
            if (room == null)
                return new NotFoundResult("Номер не найден");

            var hotels = context.ReadByAll<Hotel>();
            var hotelsHtml = new StringBuilder();
            foreach (var hotel in hotels)
            {
                var selected = (hotel.Id == room.HotelId) ? " selected" : "";
                hotelsHtml.AppendLine($"<option value=\"{hotel.Id}\"{selected}>{hotel.Name} ({hotel.City})</option>");
            }

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\room-edit.thtml",
                new
                {
                    Room = room,
                    HotelsHtml = hotelsHtml.ToString()
                }
            );
        }

        [HttpPost("admin/rooms/edit")]
        public IHttpResult UpdateRoom(
    int Id,
    int HotelId,
    string RoomType,
    decimal PricePerNight,
    int Capacity,
    string? Area,
    string? Amenities,
    string? BedType,
    bool IsAvailable)
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            if (Id <= 0)
                return new JsonResult(new { error = "Неверный ID номера." }, 400);

            // Валидация
            if (HotelId <= 0)
                return new JsonResult(new { error = "Не выбран отель." }, 400);

            if (string.IsNullOrWhiteSpace(RoomType))
                return new JsonResult(new { error = "Тип номера обязателен." }, 400);

            if (PricePerNight < 0)
                return new JsonResult(new { error = "Цена не может быть отрицательной." }, 400);

            if (Capacity <= 0)
                return new JsonResult(new { error = "Вместимость должна быть больше нуля." }, 400);

            try
            {
                var room = new Room
                {
                    Id = Id,
                    HotelId = HotelId,
                    RoomType = RoomType.Trim(),
                    PricePerNight = PricePerNight,
                    Capacity = Capacity,
                    Area = Area?.Trim(),
                    Amenities = Amenities?.Trim(),
                    BedType = BedType?.Trim(),
                    IsAvailable = IsAvailable
                };

                var context = new ORMContext(_connectionString);
                context.Update(room.Id, room);

                return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\success.thtml",
                new { Message = "Номер успешно обновлён!" }
        );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UpdateRoom failed: {ex}");
                return new JsonResult(new { error = "Ошибка: " + ex.Message }, 500);
            }
        }

        [HttpPost("admin/rooms/delete")]
        public IHttpResult DeleteRoom(int id)
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            try
            {
                var context = new ORMContext(_connectionString);
                context.Delete<Room>(id);
                return new RedirectResult("/admin/rooms");
            }
            catch (Exception ex)
            {
                return new BadRequestResult("Ошибка удаления номера: " + ex.Message);
            }
        }



        [HttpGet("admin/users")]
        public IHttpResult Users()
        {
            var authError = EnsureAdmin();
            if (authError != null) return authError;

            var context = new ORMContext(_connectionString);
            var users = context.ReadByAll<User>();

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\Admin\users.thtml",
                new { Users = users }
            );
        }
    }
}