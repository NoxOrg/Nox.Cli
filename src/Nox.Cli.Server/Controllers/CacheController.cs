using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Shared.DTO.Health;

namespace Nox.Cli.Server.Controllers;

public class CacheController : Controller
{
    private readonly INoxCliCacheManager _cacheManager;

    public CacheController(INoxCliCacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }
    
    [AllowAnonymous]
    [HttpPost("[action]")]
    public ActionResult Refresh()
    {
        
        return Ok();
    }
    
    
}