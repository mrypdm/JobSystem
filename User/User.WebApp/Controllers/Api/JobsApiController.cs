using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contract.Models;
using User.Database.Contexts;
using User.Database.Models;
using User.WebApp.Models;

namespace User.WebApp.Controllers.Api;

/// <summary>
/// API controller for user Jobs
/// </summary>
[Authorize]
[Route("api/jobs")]
[ValidateAntiForgeryToken]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class JobsApiController(UserDbContext userDbContext, HttpClient jobApiClient) : Controller
{
    /// <summary>
    /// Create new user Job
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNewJobAsync(CreateUserJobRequest request, CancellationToken cancellationToken)
    {
        var username = HttpContext.User.Claims.Single(m => m.Type == ClaimTypes.Name).Value;
        var jobId = Guid.NewGuid();

        await userDbContext.AddNewUserJobAsync(new UserJobDbModel
        {
            Username = username,
            JobId = jobId,
        }, cancellationToken);

        await jobApiClient.PostAsJsonAsync("api/jobs", new CreateJobRequest
        {
            Id = jobId,
            Timeout = request.Timeout,
            Steps = request.Steps
        }, cancellationToken);

        return CreatedAtRoute("GetJobResult", jobId, request);
    }

    /// <summary>
    /// Get all user Jobs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserJobDbModel[]>> GetUserJobsAsync(CancellationToken cancellationToken)
    {
        var username = HttpContext.User.Claims.Single(m => m.Type == ClaimTypes.Name).Value;
        return await userDbContext.GetUserJobsAsync(username, cancellationToken);
    }

    /// <summary>
    /// Get User job results
    /// </summary>
    [HttpGet("{jobId}", Name = "GetJobResult")]
    public async Task<ActionResult<JobResultResponse>> GetUserJobResults([FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var username = HttpContext.User.Claims.Single(m => m.Type == ClaimTypes.Name).Value;
        var isUserJob = await userDbContext.IsUserJobAsync(username, jobId, cancellationToken);
        return await jobApiClient.GetFromJsonAsync<JobResultResponse>($"api/jobs/{jobId}", cancellationToken);
    }
}
