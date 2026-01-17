using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace travel_agency_service.Services
{
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ReminderBackgroundService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var reminderService =
                    scope.ServiceProvider
                         .GetRequiredService<ReminderService>();

                await reminderService.SendUpcomingTripRemindersAsync();

                await Task.Delay(
                    TimeSpan.FromDays(1),
                    stoppingToken);
            }
        }
    }
}
