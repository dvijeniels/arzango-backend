namespace ArzanGo.Models.Requests
{
    public class UserUpdateModel
    {
        public Guid UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Password { get; set; }
        public bool? Courier { get; set; }
        public bool? Admin { get; set; }
        public required string FcmToken { get; set; }
    }
}
