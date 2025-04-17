namespace ArzanGo.DTO
{
    public class CategoryDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
