using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Models
{
    public class Room
    {
        public Room() { }
        public int Id { get; set; }
        public int HotelId { get; set; }
        public string RoomType { get; set; } 
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }     
        public string Area { get; set; }     
        public string Amenities { get; set; } 
        public string BedType { get; set; }   
        public bool IsAvailable { get; set; }
    }
}
