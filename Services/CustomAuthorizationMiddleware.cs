using ArzanGo.DTO;
using System.Text.Json;

namespace ArzanGo.Services
{
    public class CustomAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                var endpoint = context.GetEndpoint();
                if (endpoint?.Metadata.GetMetadata<CustomAuthorizeAttribute>() is { } attr)
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        StatusCode = 403,
                        Message = attr.CustomMessage
                    }));
                }
            }
        }
    }
}
