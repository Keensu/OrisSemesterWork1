using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniHttpServer.Models;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class RoomsEndPoint : EndpointBase
    {
        private readonly string _connectionString;
        public RoomsEndPoint()
        {
            _connectionString = "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=g7-gh-c5hc;";
        }

        [HttpGet("rooms/{hotelId}")]
        public IHttpResult GetRoomsByHotel(int hotelId)
        {
            try
            {
                ORMContext context = new ORMContext(_connectionString);
                var rooms = context.ReadByAll<Room>()
                                   .Where(r => r.HotelId == hotelId)
                                   .OrderBy(r => r.Id)
                                   .ToList();

                var hotel = context.ReadById<Hotel>(hotelId);
                string hotelName = hotel?.Name ?? $"Отель (ID={hotelId})";

                return new PageResult(
                    @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\rooms.thtml",
                    new { Rooms = rooms, HotelName = hotelName , HotelId = hotelId}
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetRoomsByHotel failed: {ex}");
                return new ErrorPageResult(500, "Внутренняя ошибка сервера: " + ex.Message);
            }
        }

        [HttpGet("rooms/get/{id}")]
        public IHttpResult GetRoomById(int id)
        {
            try
            {

                ORMContext context = new ORMContext(_connectionString);
                var room = context.ReadById<Room>(id);

                if (room == null)
                    return new NotFoundResult($"Номер с ID {id} не найден");


                var hotel = context.ReadById<Hotel>(room.HotelId);

                var safeRoom = new
                {
                    room.Id,
                    room.HotelId,
                    room.RoomType,
                    room.PricePerNight,
                    room.Capacity,
                    room.Area,
                    room.Amenities,
                    room.BedType,
                    room.IsAvailable,
                    HotelName = hotel?.Name ?? "Неизвестен"
                };

                return new PageResult(
                    @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\rooms-detail.thtml",
                    new { Room = safeRoom }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetRoomById failed: {ex}");
                return new ErrorPageResult(500, "Внутренняя ошибка сервера: " + ex.Message);
            }
        }
    }
}
