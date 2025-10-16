using System;
using System.Threading;
using System.Threading.Tasks;
using Job.Broker;
using Job.Contract;
using Job.Database.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace Job.WebApi.Controllers;

/// <summary>
/// Controller for manage jobs
/// </summary>
public class JobsController(JobDbContext jobDbContext, JobProducer jobProducer) : Controller
{
    /// <summary>
    /// Add new job
    /// </summary>
    public async Task AddNewJobAsync([FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        await jobDbContext.AddNewJobAsync(request, cancellationToken);
        await jobProducer.PublishAsync(new JobMessage() { Id = request.Id }, cancellationToken);
    }

    /// <summary>
    /// Get job results
    /// </summary>
    public async Task<JobResultResponse> GetJobResultsAsync([FromQuery] Guid jobId,
        CancellationToken cancellationToken)
    {
        return await jobDbContext.GetJobResults(jobId, cancellationToken);
    }
}
