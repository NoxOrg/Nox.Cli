using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nox.Cli.Shared.DTO.Health.Response;

namespace Nox.Cli.Server.Controllers;

[Route("[controller]/v1/")]
[Produces("application/json")]
public class HealthController : Controller
{
    [AllowAnonymous]
    [HttpGet("[action]")]
    public ActionResult<HealthResponseDto> Echo()
    {
        var assm = Assembly.GetEntryAssembly();
        var result = new HealthResponseDto
        {
            Version = assm!.GetName().Version!.ToString()
        };
        return Ok(result);
    }
}