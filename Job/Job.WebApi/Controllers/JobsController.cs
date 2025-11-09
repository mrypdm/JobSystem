using System.Buffers.Text;
using Job.Broker;
using Job.Broker.Producers;
using Job.Contract;
using Job.Database.Contexts;
using Job.WebApi.Options;
using Microsoft.AspNetCore.Mvc;

namespace Job.WebApi.Controllers;

/// <summary>
/// Controller for manage jobs
/// </summary>
[Route("api/jobs")]
public class JobsController(IJobDbContext jobDbContext, IJobProducer jobProducer, JobsControllerOptions options)
    : Controller
{
    /// <summary>
    /// Add new job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<NewJobModel>> AddNewJobAsync([FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
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

        if (string.IsNullOrWhiteSpace(newJob.Script))
        {
            return BadRequest("Job script cannot be empty");
        }

        if (!Base64.IsValid(newJob.Script))
        {
            return BadRequest("Job script must be base64 encoded");
        }

        await jobDbContext.AddNewJobAsync(newJob, cancellationToken);
        await jobProducer.PublishAsync(new JobMessage() { Id = newJob.Id }, cancellationToken);

        return newJob;
    }

    /// <summary>
    /// Get job results
    /// </summary>
    [HttpGet("{jobId}", Name = "GetJobResult")]
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
