using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class SearchEndpoint : EndpointBase
    {
        private readonly string _connectionString = "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=g7-gh-c5hc;";

        [HttpGet("search")]
        public IHttpResult Search()
        {
            var country = Context.Request.QueryString["country"] ?? "";
            var city = Context.Request.QueryString["city"] ?? "";
            var minStarsStr = Context.Request.QueryString["minStars"];
            var maxPriceStr = Context.Request.QueryString["maxPrice"];
            var checkIn = Context.Request.QueryString["checkIn"] ?? "";
            var checkOut = Context.Request.QueryString["checkOut"] ?? "";

            int? minStars = null;
            if (!string.IsNullOrEmpty(minStarsStr) && int.TryParse(minStarsStr, out int ms))
                minStars = ms;

            decimal? maxPrice = null;
            if (!string.IsNullOrEmpty(maxPriceStr) && decimal.TryParse(maxPriceStr, out decimal mp))
                maxPrice = mp;

            var context = new ORMContext(_connectionString);
            var hotels = context.ReadByAll<Hotel>().AsEnumerable();


            if (!string.IsNullOrWhiteSpace(city))
            {
                var c = city.Trim().ToLower();
                hotels = hotels.Where(h => !string.IsNullOrEmpty(h.City) && h.City.Trim().ToLower().Contains(c));
            }


            if (!string.IsNullOrWhiteSpace(country))
            {
                var c = country.Trim().ToLower();
                hotels = hotels.Where(h => !string.IsNullOrEmpty(h.Country) && h.Country.Trim().ToLower().Contains(c));
            }


            if (minStars.HasValue)
                hotels = hotels.Where(h => h.Stars >= minStars.Value);

            var hotelList = hotels.OrderBy(h => h.Id).ToList();

            var allRooms = context.ReadByAll<Room>().ToList();


            var minPriceByHotel = allRooms
                .Where(r => r.HotelId > 0)
                .GroupBy(r => r.HotelId)
                .ToDictionary(g => g.Key, g => g.Min(r => r.PricePerNight));


            if (maxPrice.HasValue)
            {
                var validHotelIds = minPriceByHotel
                    .Where(kvp => kvp.Value <= maxPrice.Value)
                    .Select(kvp => kvp.Key)
                    .ToHashSet();

                hotelList = hotelList.Where(h => validHotelIds.Contains(h.Id)).ToList();
            }

            var hotelsWithPriceText = hotelList.Select(hotel => new
            {
                Id = hotel.Id,
                Name = hotel.Name,
                City = hotel.City,
                Country = hotel.Country,
                Stars = hotel.Stars,
                Phone = hotel.Phone,
                MainPhoto = hotel.MainPhoto ?? "default.jpg",
                PriceDisplay = minPriceByHotel.TryGetValue(hotel.Id, out var price)
                    ? $"{price} руб./ночь"
                    : "Цена не указана — нет доступных номеров"
            }).ToList();

            var model = new
            {
                Hotels = hotelsWithPriceText,
                Country = country,
                City = city,
                MinStars = minStars,
                MaxPrice = maxPrice?.ToString() ?? "",
                CheckIn = checkIn,
                CheckOut = checkOut
            };

            return new PageResult(
                @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\search.thtml",
                model
            );
        }
    }
}