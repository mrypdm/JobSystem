using System;
using System.Threading;
using Contract;
using Job.Contract;
using Job.Database.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace Job.WebApi.Controllers;

/// <summary>
/// Controller for manage jobs
/// </summary>
public class JobsController(JobDbContext jobDbContext) : Controller
{
    /// <summary>
    /// Add new job
    /// </summary>
    public ActionResult AddNewJobAsync([FromBody] PostJobRequest request,
        CancellationToken cancellationToken)
    {
        return default;
    }

    /// <summary>
    /// Get job results
    /// </summary>
    public ActionResult<JobResultResponse> GetJobResultsAsync([FromQuery] Guid jobId,
        CancellationToken cancellationToken)
    {
        return default;
    }
}
