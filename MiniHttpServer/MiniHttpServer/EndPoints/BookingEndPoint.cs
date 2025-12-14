using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class BookingsEndPoint : EndpointBase
    {
        private readonly string _connectionString;
        public BookingsEndPoint()
        {
            _connectionString = "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=g7-gh-c5hc;";
        }

        [HttpGet("bookings/user/{userId}")]
        public IHttpResult GetUserBookings(int userId)
        {
            ORMContext context = new ORMContext(_connectionString);
            var bookings = context.ReadByAll<Booking>().Where(b => b.UserId == userId).ToList();

            var bookingsWithDetails = bookings.Select(b =>
            {
                var room = context.ReadById<Room>(b.RoomId);
                var hotel = context.ReadById<Hotel>(room?.HotelId ?? 0);
                return new
                {
                    b.Id,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.TotalPrice,
                    b.Status,
                    b.CreatedAt,
                    RoomName = room?.RoomType,
                    HotelName = hotel?.Name
                };
            }).ToList();

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\bookings.thtml", new { Bookings = bookingsWithDetails });
        }

        [HttpPost("bookings/create")]
        public IHttpResult CreateBooking(int RoomId, string CheckInDate, string CheckOutDate)
        {
            var userId = GetUserIdFromCookie();
            if (userId == null)
                return new UnauthorizedResult("Authentication required");

            
            if (!DateTime.TryParse(CheckInDate, out DateTime checkIn) ||
                !DateTime.TryParse(CheckOutDate, out DateTime checkOut))
            {
                return new BadRequestResult("Invalid date format");
            }

            if (RoomId <= 0 || checkIn >= checkOut)
                return new BadRequestResult("Invalid data");

            var context = new ORMContext(_connectionString);

            var room = context.ReadById<Room>(RoomId);
            if (room == null || !room.IsAvailable)
                return new ConflictResult("Room is not available");

            var days = (checkOut - checkIn).Days;
            var totalPrice = days * room.PricePerNight;

            var booking = new Booking
            {
                UserId = userId.Value,
                RoomId = RoomId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                TotalPrice = totalPrice,
                Status = "confirmed"
            };

            context.Create(booking);

            room.IsAvailable = false;
            context.Update(RoomId, room);

            return new RedirectResult($"/bookings/user/{userId}");
        }

        [HttpPost("bookings/cancel")]
        public IHttpResult CancelBooking(int id)
        {
            var userId = GetUserIdFromCookie();
            if (userId == null)
                return new UnauthorizedResult("Authentication required");

            ORMContext context = new ORMContext(_connectionString);
            var booking = context.ReadById<Booking>(id);

            if (booking == null)
                return new NotFoundResult($"Booking with id {id} not found");

            if (booking.Status == "cancelled")
                return new BadRequestResult("Booking is already cancelled");

            if (booking.UserId != userId.Value)
                return new UnauthorizedResult("You can only cancel your own bookings");

            var room = context.ReadById<Room>(booking.RoomId);
            if (room != null)
            {
                room.IsAvailable = true;
                context.Update(room.Id, room);
            }

            booking.Status = "cancelled";
            context.Update(id, booking);

            return new RedirectResult($"/bookings/user/{userId}");
        }
    }



    public class BookingRequest
    {
        public int RoomId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }

}
