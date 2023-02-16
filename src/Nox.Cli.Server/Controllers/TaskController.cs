using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nox.Cli.Abstractions;
using Nox.Cli.Server.Abstractions;
using Nox.Cli.Server.Services;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Controllers;

[Authorize]
[Route("[controller]/v1/")]
[Produces("application/json")]
public class TaskController : Controller
{
    private readonly IWorkflowContextFactory _contextFactory;

    public TaskController(
        IWorkflowContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    
    [HttpGet("[action]/{taskExecutorId}")]
    public ActionResult<ExecuteTaskResult> GetState(Guid workflowId)
    {
        //var executor = _executorFactory.GetInstance(taskExecutorId);
        var result = new TaskStateResponse
        {
            //TaskExecutorId = taskExecutorId,
            //WorkflowId = executor.WorkflowId,
            //State = executor.State,
            //StateName = Enum.GetName(executor.State)
        };
        return Ok(result);
    }


    [HttpPost("[action]")]
    public async Task<ActionResult<ExecuteTaskResult>> Execute([FromBody] ExecuteTaskRequest request)
    {
        var context = _contextFactory.GetInstance(request.WorkflowId);
        if (context == null)
        {
            context = _contextFactory.NewInstance(request.WorkflowId);
        }

        var result = context.ExecuteTask(request.ActionConfiguration!);
        return Ok(result);
    }
}