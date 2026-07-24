namespace ViscaCamLink.Services;

using System.Threading;
using System.Threading.Tasks;

public interface IStartupUpdateCheckService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
