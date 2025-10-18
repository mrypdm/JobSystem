using Job.Broker;
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
public class JobsController(JobDbContext jobDbContext, JobProducer jobProducer) : Controller
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
    public async Task<JobResultResponse> GetJobResultsAsync([FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        return await jobDbContext.GetJobResults(jobId, cancellationToken);
    }
}
