using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Email;

public class EmailQueueWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailQueueWorker> _logger;

    public EmailQueueWorker(IServiceScopeFactory scopes, IOptions<EmailOptions> options, ILogger<EmailQueueWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Worker.Enabled)
        {
            _logger.LogInformation("Email worker is off. Process the queue from Admin > Email queue, or set Email:Worker:Enabled to true.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _options.Worker.IntervalSeconds)));
        do
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IEmailQueueProcessor>();
                EmailProcessResult result;
                do
                {
                    result = await processor.ProcessAsync(_options.Worker.BatchSize, stoppingToken);
                }
                while (result.Claimed == _options.Worker.BatchSize && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email worker run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
