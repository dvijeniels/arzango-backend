using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

public enum FirebaseProjectType
{
    Users,
    Couriers
}

public class FirebaseSettings
{
    public string UsersConfigPath { get; set; } = string.Empty;
    public string CouriersConfigPath { get; set; } = string.Empty;
}

public class FirebaseNotificationService : IDisposable
{
    private readonly ILogger<FirebaseNotificationService> _logger;
    private readonly FirebaseApp _usersApp;
    private readonly FirebaseApp _couriersApp;

    public FirebaseNotificationService(
        ILogger<FirebaseNotificationService> logger,
        IOptions<FirebaseSettings> firebaseSettings,
        IWebHostEnvironment env)
    {
        _logger = logger;
        var settings = firebaseSettings.Value;

        var usersPath = Path.Combine(env.ContentRootPath, settings.UsersConfigPath);
        var couriersPath = Path.Combine(env.ContentRootPath, settings.CouriersConfigPath);

        if (!File.Exists(usersPath))
            throw new FileNotFoundException($"Firebase config file for users not found at {usersPath}");

        if (!File.Exists(couriersPath))
            throw new FileNotFoundException($"Firebase config file for couriers not found at {couriersPath}");

        _usersApp = FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(usersPath)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging")
        }, "UsersApp");

        _couriersApp = FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(couriersPath)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging")
        }, "CouriersApp");
    }

    public async Task<bool> SendNotificationToUserAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        FirebaseProjectType projectType = FirebaseProjectType.Users)
    {
        // Проверяем валидность токена
        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            _logger.LogWarning($"Empty or invalid device token for {projectType} project");
            return false;
        }

        try
        {
            var message = CreateMessage(deviceToken, title, body, data);
            var messaging = GetMessagingInstance(projectType);

            var response = await messaging.SendAsync(message);
            _logger.LogInformation($"Successfully sent notification to {deviceToken}. Message ID: {response}");
            return true;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
        {
            _logger.LogWarning($"Invalid device token for {projectType} project: {deviceToken}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to {deviceToken}");
            return false;
        }
    }

    public async Task<bool> SendNotificationToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        FirebaseProjectType projectType = FirebaseProjectType.Users)
    {
        try
        {
            var message = new Message()
            {
                Topic = topic,
                Notification = new Notification { Title = title, Body = body },
                Data = data
            };

            var messaging = GetMessagingInstance(projectType);
            var response = await messaging.SendAsync(message);

            _logger.LogInformation($"Successfully sent notification to topic {topic}. Message ID: {response}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to topic {topic}");
            return false;
        }
    }

    private FirebaseMessaging GetMessagingInstance(FirebaseProjectType projectType)
    {
        return projectType == FirebaseProjectType.Users
            ? FirebaseMessaging.GetMessaging(_usersApp)
            : FirebaseMessaging.GetMessaging(_couriersApp);
    }

    private Message CreateMessage(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data)
    {
        return new Message()
        {
            Token = deviceToken,
            Notification = new Notification { Title = title, Body = body },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = "high_importance_channel",
                    Sound = "default"
                }
            },
            Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string> { { "apns-priority", "10" } },
                Aps = new Aps { Sound = "default" }
            }
        };
    }

    public void Dispose()
    {
        _usersApp?.Delete();
        _couriersApp?.Delete();
    }
}