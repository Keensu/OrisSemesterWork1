using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.Models;
using System.Net;

[Endpoint]
internal class HomeEndPoint : EndpointBase
{
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=tours;Username=postgres;Password=g7-gh-c5hc;";

    [HttpGet("home")]
    public IHttpResult Index(
    string city = null,
    string checkIn = null,
    string checkOut = null,
    int? minStars = null)
    {
        var userId = GetUserIdFromCookie();
        dynamic model = new System.Dynamic.ExpandoObject();

        if (userId.HasValue)
        {
            var context = new ORMContext(_connectionString);
            var user = context.ReadById<User>(userId.Value);
            model.IsAuthenticated = user != null;
            model.User = user != null
                ? new { UserName = user.UserName, Role = user.Role }
                : new { UserName = "Гость", Role = "" };
        }
        else
        {
            model.IsAuthenticated = false;
            model.User = new { UserName = "Гость", Role = "" };
        }

        var context2 = new ORMContext(_connectionString);
        var allHotels = context2.ReadByAll<Hotel>().AsQueryable();
        var allRooms = context2.ReadByAll<Room>();

        var roomGroups = allRooms
            .GroupBy(r => r.HotelId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var filteredHotels = allHotels.AsQueryable();

        if (!string.IsNullOrEmpty(city))
        {
            filteredHotels = filteredHotels.Where(h =>
                h.City.Contains(city, StringComparison.OrdinalIgnoreCase) ||
                h.Name.Contains(city, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (minStars.HasValue)
        {
            filteredHotels = filteredHotels.Where(h => h.Stars >= minStars.Value);
        }


        
        var hotelsWithPrice = filteredHotels.Select(h => new
        {
            h.Id,
            h.Name,
            h.City,
            h.Country,
            h.Stars,
            h.MainPhoto,
            MinPrice = GetMinRoomPrice(h.Id, roomGroups),
            HasPrice = GetMinRoomPrice(h.Id, roomGroups).HasValue
        }).ToList();

        model.Hotels = hotelsWithPrice; 

        var countryStats = allHotels
            .GroupBy(h => h.Country)
            .Select(g => new CountryStats { Name = g.Key, HotelCount = g.Count() })
            .OrderByDescending(x => x.HotelCount)
            .Take(20)
            .ToList();

        model.CountryStats = countryStats;

        var moscowHotelsRaw = allHotels
            .Where(h => h.City != null && h.City.Contains("Москва", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        var spbHotelsRaw = allHotels
            .Where(h => h.City != null &&
                       (h.City.Contains("Санкт-Петербург", StringComparison.OrdinalIgnoreCase) ||
                        h.City.Contains("Санкт Петербург", StringComparison.OrdinalIgnoreCase)))
            .Take(3)
            .ToList();

        var moscowHotels = moscowHotelsRaw.Select(h => new
        {
            h.Id,
            h.Name,
            h.Stars,
            h.City,
            h.Country,
            h.MainPhoto, 
            MinPrice = GetMinRoomPrice(h.Id, roomGroups),
            HasPrice = GetMinRoomPrice(h.Id, roomGroups).HasValue
        }).ToList();

        var spbHotels = spbHotelsRaw.Select(h => new
        {
            h.Id,
            h.Name,
            h.Stars,
            h.City,
            h.Country,
            h.MainPhoto, 
            MinPrice = GetMinRoomPrice(h.Id, roomGroups),
            HasPrice = GetMinRoomPrice(h.Id, roomGroups).HasValue
        }).ToList();

        model.ShowMoscowHotels = moscowHotels.Count > 0;
        model.MoscowHotels = moscowHotels;

        model.ShowSpbHotels = spbHotels.Count > 0;
        model.SpbHotels = spbHotels;

        return new PageResult(
            @"C:\Users\yar10\source\repos\MiniHttpServer\MiniHttpServer\Template\Page\main.thtml",
            model
        );
    }

    [HttpPost("home/logout")]
    public IHttpResult Logout()
    {
        var cookieValue = "UserId=; Path=/; Expires=Thu, 01 Jan 1970 00:00:00 GMT";
        Context.Response.Headers.Add("Set-Cookie", cookieValue);
        return new RedirectResult("/home"); 
    }

    private decimal? GetMinRoomPrice(int hotelId, Dictionary<int, List<Room>> roomGroups)
    {
        if (roomGroups.TryGetValue(hotelId, out var rooms) && rooms.Any())
        {
            return rooms.Min(r => r.PricePerNight);
        }
        return null;
    }



}