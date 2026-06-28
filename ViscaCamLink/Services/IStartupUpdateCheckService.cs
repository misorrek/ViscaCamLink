namespace ViscaCamLink.Services;

public interface IStartupUpdateCheckService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}