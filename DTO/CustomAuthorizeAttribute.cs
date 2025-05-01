using Microsoft.AspNetCore.Authorization;

namespace ArzanGo.DTO
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public string CustomMessage { get; set; } = "Доступ запрещен. Необходимы права администратора. Admin olarak giriş yap önce";

        public CustomAuthorizeAttribute() : base() { }

        public CustomAuthorizeAttribute(string roles) : base(roles)
        {
            if (roles.Contains("Admin"))
                CustomMessage = "Доступ запрещен. Необходимы права администратора. Admin olarak giriş yap önce";
        }
    }
}
