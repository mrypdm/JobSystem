using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Contract.Models;
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
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class JobsApiController(UserDbContext userDbContext, HttpClient jobApiClient) : Controller
{
    /// <summary>
    /// Create new user Job
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNewJobAsync(CreateUserJobRequest request, CancellationToken cancellationToken)
    {
        var username = HttpContext.GetUserName();
        var jobId = Guid.NewGuid();

        await userDbContext.AddNewUserJobAsync(username, jobId, cancellationToken);
        await jobApiClient.PostAsJsonAsync("api/jobs", new CreateJobRequest
        {
            Id = jobId,
            Timeout = request.Timeout,
            Script = await ReadFileAsBase64(request.Script, cancellationToken)
        }, cancellationToken);

        return CreatedAtRoute("GetJobResult", jobId, request);
    }

    /// <summary>
    /// Get User job results
    /// </summary>
    [HttpGet("{jobId}", Name = "GetJobResult")]
    public async Task<ActionResult<JobResultResponse>> GetUserJobResults([FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var username = HttpContext.GetUserName();
        var isUserJob = await userDbContext.IsUserJobAsync(username, jobId, cancellationToken);
        if (!isUserJob)
        {
            return StatusCode(404, "Wrong Job");
        }

        return await jobApiClient.GetFromJsonAsync<JobResultResponse>($"api/jobs/{jobId}", cancellationToken);
    }

    private static async Task<string> ReadFileAsBase64(IFormFile file, CancellationToken cancellationToken)
    {
        using var base64Stream = new CryptoStream(file.OpenReadStream(), new ToBase64Transform(), CryptoStreamMode.Read);
        using var streamReader = new StreamReader(base64Stream);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }
}
