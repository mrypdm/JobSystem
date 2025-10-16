using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User.Database.Contexts;
using User.WebApp.Extensions;
using User.WebApp.Views.Jobs;

namespace User.WebApp.Controllers;

/// <summary>
/// Jobs UI controller
/// </summary>
[Authorize]
[Route("")]
public class JobsController(UserDbContext userDbContext) : Controller
{
    /// <summary>
    /// Get common view with all jobs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetViewAsync(CancellationToken cancellationToken)
    {
        var username = HttpContext.GetUserName();
        var userJobs = await userDbContext.GetUserJobsAsync(username, cancellationToken);
        return View("Index", new IndexModel(userJobs));
    }

    /// <summary>
    /// Get view with job results
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public ActionResult GetJobResultsView([FromRoute] Guid jobId)
    {
        return View("JobResults", new JobResultsModel(jobId));
    }

    /// <summary>
    /// Get view for Job creation
    /// </summary>
    [HttpGet("jobs/create")]
    public ActionResult GetJobCreateView()
    {
        return View("JobCreation");
    }
}
