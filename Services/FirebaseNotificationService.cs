using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

public class FirebaseNotificationService
{
    private readonly ILogger<FirebaseNotificationService> _logger;

    public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
    {
        _logger = logger;

        // Инициализация Firebase (выполняется один раз)
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("firebase-service-account.json")
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging")
            });
        }
    }

    public async Task SendNotificationToUserAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
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
                    Headers = new Dictionary<string, string>
                    {
                        { "apns-priority", "10" }
                    },
                    Aps = new Aps
                    {
                        Sound = "default"
                    }
                }
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation($"Successfully sent notification to {deviceToken}. Message ID: {response}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to {deviceToken}");
            throw;
        }
    }

    public async Task SendNotificationToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var message = new Message()
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation($"Successfully sent notification to topic {topic}. Message ID: {response}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to topic {topic}");
            throw;
        }
    }
}