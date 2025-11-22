using System.Security.Cryptography;
using Job.Contract;
using Job.WebApi.Client.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User.Database.Contexts;
using User.WebApp.Extensions;
using User.WebApp.Models;

namespace User.WebApp.Controllers.Api;

/// <summary>
/// API controller for user Jobs
/// </summary>
[Authorize]
[Route("api/jobs")]
[ValidateAntiForgeryToken]
public class JobApiController(IUserDbContext userDbContext, IJobWebApiClient jobWebApiClient) : Controller
{
    /// <summary>
    /// Create new user Job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateNewJobAsync([FromForm] CreateUserJobRequest request,
        CancellationToken cancellationToken)
    {
        var jobId = await jobWebApiClient.CreateNewJobAsync(new CreateJobRequest
        {
            Timeout = request.Timeout,
            Script = await ReadFileAsBase64(request.Script, cancellationToken)
        }, cancellationToken);
        await userDbContext.AddNewUserJobAsync(HttpContext.GetUserName(), jobId, cancellationToken);

        return CreatedAtRoute("GetJobResult", new { jobId }, new JobResultResponse());
    }

    /// <summary>
    /// Get User job results
    /// </summary>
    [HttpGet("{jobId}", Name = "GetJobResult")]
    public async Task<ActionResult<JobResultResponse>> GetUserJobResultsAsync([FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var username = HttpContext.GetUserName();
        var isUserJob = await userDbContext.IsUserJobAsync(username, jobId, cancellationToken);
        if (!isUserJob)
        {
            return NotFound($"Cannot found Job '{jobId}'");
        }

        return await jobWebApiClient.GetJobResultsAsync(jobId, cancellationToken);
    }

    private static async Task<string> ReadFileAsBase64(IFormFile file, CancellationToken cancellationToken)
    {
        using var base64Stream = new CryptoStream(file.OpenReadStream(), new ToBase64Transform(), CryptoStreamMode.Read);
        using var streamReader = new StreamReader(base64Stream);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }
}
