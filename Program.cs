using ArzanGo.Data;
using ArzanGo.Models;
using ArzanGo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Регистрируем сам DbContext (Scoped)
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        //options.Events = new JwtBearerEvents
        //{
        //    OnMessageReceived = context =>
        //    {
        //        var accessToken = context.Request.Query["access_token"];
        //        if (!string.IsNullOrEmpty(accessToken) &&
        //            context.HttpContext.Request.Path.StartsWithSegments("/ws"))
        //        {
        //            context.Token = accessToken;
        //        }
        //        return Task.CompletedTask;
        //    }
        //};
    });

// Добавляем политику, требующую аутентификацию по умолчанию
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CourierOnly", policy => policy.RequireRole("Courier"));
    options.AddPolicy("AdminOrCourier", policy => policy.RequireRole("Admin", "Courier"));
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IPaymentService, PaymentService>();

// В методе Configure или при инициализации приложения

builder.Services.AddSingleton<FirebaseNotificationService>();

builder.Services.AddSingleton<WebSocketHandler>(provider =>
{
    var contextFactory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    var logger = provider.GetRequiredService<ILogger<WebSocketHandler>>();
    return new WebSocketHandler(contextFactory, logger);
});

builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.PaymentSettings.Any())
    {
        db.PaymentSettings.AddRange(
            new PaymentSettings
            {
                Name = "Наличными",
                IsActive = true,
                DisplayOrder = 1
            },
            new PaymentSettings
            {
                Name = "Банковской картой",
                IsActive = true,
                DisplayOrder = 2
            },
            new PaymentSettings
            {
                Name = "Переводом",
                IsActive = true,
                DisplayOrder = 3
            }
        );
        db.SaveChanges();
    }
}

app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest && context.Request.Path == "/ws")
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = app.Services.GetRequiredService<WebSocketHandler>();
        await handler.HandleWebSocketAsync(webSocket);
    }
    else
    {
        await next();
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        options.RoutePrefix = string.Empty;
    });
}
app.UseCors(builder =>
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
);
app.UseMiddleware<CustomAuthorizationMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

app.Run();
