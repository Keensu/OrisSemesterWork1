using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using System;
using System.Linq;
using MiniHttpServer.Models;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class HotelsEndPoint : EndpointBase
    {
        private readonly string _connectionString;
        public HotelsEndPoint()
        {
            _connectionString = "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=g7-gh-c5hc;";
        }

        [HttpGet("hotels/get")]
        public IHttpResult GetHotels()
        {
            ORMContext context = new ORMContext(_connectionString);
            var hotels = context.ReadByAll<Hotel>().OrderBy(h => h.Id).ToList();

            var safeHotels = hotels.Select(h => new
            {
                h.Id,
                h.Name,
                h.Description,
                h.Address,
                h.City,
                h.Country,
                h.Phone,
                h.Email,
                h.Stars,
                h.Photos
                
            });

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\hotels.thtml", new { Hotels = safeHotels });
        }

        [HttpGet("hotels/get/{id}")]
        public IHttpResult GetHotelById(int id)
        {
            ORMContext context = new ORMContext(_connectionString);
            var hotel = context.ReadById<Hotel>(id);

            if (hotel == null)
                return new NotFoundResult($"Hotel with id {id} not found");

            var safeHotel = new
            {
                hotel.Id,
                hotel.Name,
                hotel.Description,
                hotel.Address,
                hotel.City,
                hotel.Country,
                hotel.Phone,
                hotel.Email,
                hotel.Stars,
                hotel.Photos
            };

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\hotel-detail.thtml", new { Hotel = safeHotel });
        }

        [HttpGet("hotels/search")]
        public IHttpResult SearchHotels(string city = null, string country = null, int? minStars = null, int? maxStars = null)
        {
            ORMContext context = new ORMContext(_connectionString);
            var query = context.ReadByAll<Hotel>().OrderBy(h => h.Id).AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(h => h.City.Contains(city, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(country))
                query = query.Where(h => h.Country.Contains(country, StringComparison.OrdinalIgnoreCase));

            if (minStars.HasValue)
                query = query.Where(h => h.Stars >= minStars.Value);

            if (maxStars.HasValue)
                query = query.Where(h => h.Stars <= maxStars.Value);

            var hotels = query.ToList();

            var safeHotels = hotels.Select(h => new
            {
                h.Id,
                h.Name,
                h.Description,
                h.Address,
                h.City,
                h.Country,
                h.Phone,
                h.Email,
                h.Stars,
                h.Photos
            });

            return new PageResult(@"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\hotels.thtml", new { Hotels = safeHotels });
        }
    }

}