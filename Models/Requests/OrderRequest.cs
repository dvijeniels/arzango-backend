namespace ArzanGo.Models.Requests
{
    public class OrderRequest
    {
        public string? BuyingType { get; set; }
        public string? Comment { get; set; }
        public required Guid AddressId { get; set; }
    }
}
