using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Models
{
    public class Hotel
    {
        public Hotel() { }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int Stars { get; set; }
        public string[] Photos { get; set; }

        public string? MainPhoto => Photos?.Length > 0 ? Photos[0] : null;
    }
    public class CountryStats
    {
        public string Name { get; set; }
        public int HotelCount { get; set; }
    }
    
    public class CityStats
    {
        public string Name { get; set; }
        public int HotelCount { get; set; }
    }

}
