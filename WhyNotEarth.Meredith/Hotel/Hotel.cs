using System.Collections.Generic;
using WhyNotEarth.Meredith.Public;

namespace WhyNotEarth.Meredith.Hotel
{
    public class Hotel
    {
        public int Id { get; set; }

        public int? CompanyId { get; set; }

        public Company? Company { get; set; }

        public ICollection<Amenity>? Amenities { get; set; }

        public int PageId { get; set; }

        public Page Page { get; set; } = null!;

        public ICollection<RoomType>? RoomTypes { get; set; }

        public ICollection<Rule>? Rules { get; set; } = null!;

        public ICollection<Space>? Spaces { get; set; } = null!;

        public ICollection<HotelTranslation>? Translations { get; set; }
    }
}