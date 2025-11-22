using System.Buffers.Text;
using Job.Broker;
using Job.Contract;
using Job.Database.Contexts;
using Job.WebApi.Options;
using Microsoft.AspNetCore.Mvc;
using Shared.Broker.Abstractions;
using Shared.Contract.Owned;

namespace Job.WebApi.Controllers;

/// <summary>
/// Controller for manage jobs
/// </summary>
[Route("api/jobs")]
public class JobController(IOwnedService<IJobDbContext> jobDbContextOwned, IBrokerProducer<Guid, JobMessage> jobProducer,
    JobControllerOptions options, ILogger<JobController> logger)
    : Controller
{
    /// <summary>
    /// Add new job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> AddNewJobAsync([FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Job creation request cannot be empty");
        }

        if (request.Id is null)
        {
            logger.LogInformation("Job is created without Id. It will be generated");
        }

        if (request.Timeout is null)
        {
            logger.LogInformation("Job is created without Timeout. Default [{DefaultTimeout}] value will be used",
                options.DefaultTimeout);
        }

        var newJob = new NewJobModel
        {
            Id = request.Id ?? Guid.NewGuid(),
            Timeout = request.Timeout ?? options.DefaultTimeout,
            Script = request.Script
        };

        if (newJob.Timeout > options.MaxTimeout)
        {
            return BadRequest($"Maximum allowed timeout for Job is '{options.MaxTimeout}'");
        }

        if (newJob.Timeout <= TimeSpan.Zero)
        {
            return BadRequest("Timeout must be positive");
        }

        if (string.IsNullOrWhiteSpace(newJob.Script))
        {
            return BadRequest("Job script cannot be empty");
        }

        if (!Base64.IsValid(newJob.Script))
        {
            return BadRequest("Job script must be base64 encoded");
        }

        using var jobDbContext = jobDbContextOwned.Value;
        await jobDbContext.AddNewJobAsync(newJob, cancellationToken);
        await jobProducer.PublishAsync(new JobMessage() { Id = newJob.Id }, cancellationToken);

        return newJob.Id;
    }

    /// <summary>
    /// Get job results
    /// </summary>
    [HttpGet("{jobId}", Name = "GetJobResult")]
    public async Task<ActionResult<JobResultResponse>> GetJobResultsAsync([FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        using var jobDbContext = jobDbContextOwned.Value;
        var results = await jobDbContext.GetJobResultsAsync(jobId, cancellationToken);

        if (results is null)
        {
            return NotFound($"Cannot found results for job '{jobId}'");
        }

        return results;
    }
}
