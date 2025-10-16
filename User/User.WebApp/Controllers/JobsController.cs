using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace User.WebApp.Controllers;

/// <summary>
/// Jobs UI controller
/// </summary>
[Authorize]
[Route("")]
public class JobsController : Controller
{
    /// <summary>
    /// Get generator view
    /// </summary>
    [HttpGet]
    public ActionResult GetView()
    {
        return View("Jobs");
    }
}
