using ArzanGo.Models;

namespace ArzanGo.DTO
{
    public class AddressDto
    {
        public Guid AddressId { get; set; }
        public string City { get; set; } = string.Empty;
        public City CityEnum { get; set; }
        public string Street { get; set; } = string.Empty;
        public string House { get; set; } = string.Empty;
        public string? Additionally { get; set; }
        public string? PostalCode { get; set; }
        public Guid UserId { get; set; }
    }
}
