using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nox.Workflow;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Net;

namespace Nox.Cli.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RunWorkflowController : ControllerBase
    {
        private readonly ILogger<RunWorkflowController> _logger;

        public RunWorkflowController(ILogger<RunWorkflowController> logger)
        {
            _logger = logger;
        }

        [HttpPost(Name = "RunWorkflow")]
        public async Task<IActionResult> Post([FromBody]NoxWorkflowParameters workflowParameters)
        {
            var executor = new NoxWorkflowExecutor(workflowParameters, new HttpResponseConsole());

            return await executor.Execute() ? Ok() : StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}


public class HttpResponseConsole : IAnsiConsole
{
    public Profile Profile => throw new NotImplementedException();

    public IAnsiConsoleCursor Cursor => throw new NotImplementedException();

    public IAnsiConsoleInput Input => throw new NotImplementedException();

    public IExclusivityMode ExclusivityMode => throw new NotImplementedException();

    public RenderPipeline Pipeline => throw new NotImplementedException();

    public void Clear(bool home)
    {
        throw new NotImplementedException();
    }

    public void Write(IRenderable renderable)
    {
        throw new NotImplementedException();
    }
}