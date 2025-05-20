namespace ArzanGo.Services
{
    public interface IKyrgyzstanTimeService
    {
        DateTime Now { get; }
    }

    public class KyrgyzstanTimeService : IKyrgyzstanTimeService
    {
        public DateTime Now
        {
            get
            {
                var kgTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kgTimeZone);
            }
        }
    }
}
