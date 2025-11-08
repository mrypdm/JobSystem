using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User.Database.Contexts;
using User.WebApp.Extensions;
using User.WebApp.Models;

namespace User.WebApp.Controllers;

/// <summary>
/// Jobs UI controller
/// </summary>
[Authorize]
[Route("")]
public class JobsController(IUserDbContext userDbContext) : Controller
{
    /// <summary>
    /// Get common view with all jobs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetViewAsync(CancellationToken cancellationToken)
    {
        var username = HttpContext.GetUserName();
        var userJobs = await userDbContext.GetUserJobsAsync(username, cancellationToken);
        return View("Index", userJobs);
    }

    /// <summary>
    /// Get view with job results
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public ActionResult GetJobResultsView([FromRoute] Guid jobId)
    {
        return View("JobResults", jobId);
    }

    /// <summary>
    /// Get view for Job creation
    /// </summary>
    [HttpGet("jobs/create")]
    public ActionResult GetJobCreateView()
    {
        return View("JobCreation", new CreateUserJobRequest());
    }
}
