using System.Threading;
using System.Threading.Tasks;

namespace Job.Broker;

/// <summary>
/// Handler for <see cref="JobMessage"/>
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Handle consumed message
    /// </summary>
    Task HandleAsync(JobMessage jobMessage, CancellationToken cancellationToken);
}
