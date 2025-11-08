using Job.Broker;
using Job.Broker.Producers;
using Job.Contract;
using Job.Database.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Job.WebApi.Controllers;

/// <summary>
/// Controller for manage jobs
/// </summary>
[Authorize]
[Route("api/jobs")]
public class JobsController(IJobDbContext jobDbContext, IJobProducer jobProducer) : Controller
{
    /// <summary>
    /// Add new job
    /// </summary>
    [HttpPost]
    public async Task AddNewJobAsync([FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        await jobDbContext.AddNewJobAsync(request, cancellationToken);
        await jobProducer.PublishAsync(new JobMessage() { Id = request.Id }, cancellationToken);
    }

    /// <summary>
    /// Get job results
    /// </summary>
    [HttpGet("{jobId}")]
    public async Task<ActionResult<JobResultResponse>> GetJobResultsAsync([FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var results = await jobDbContext.GetJobResults(jobId, cancellationToken);

        if (results is null)
        {
            return NotFound($"Cannot found results for job '{jobId}'");
        }

        return results;
    }
}
