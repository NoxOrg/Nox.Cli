using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nox.Cli.Abstractions;
using Nox.Cli.Server.Services;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Controllers;

[Authorize]
[Route("[controller]/v1/")]
[Produces("application/json")]
public class TaskController : Controller
{
    private readonly ITaskExecutorFactory _executorFactory;

    public TaskController(
        ITaskExecutorFactory executorFactory)
    {
        _executorFactory = executorFactory;
    }
    
    
    [HttpGet("[action]/{workflowId}")]
    public ActionResult<ExecuteTaskResponse> GetState(Guid workflowId)
    {
        var result = new TaskStateResponse
        {
            WorkflowId = workflowId,
            //TODO change this to actual state
            State = ActionState.Running
        };
        return Ok(result);
    }
    
    [HttpPost("[action]")]
    public async Task<ActionResult<ExecuteTaskResponse>> Begin([FromBody] BeginTaskRequest request)
    {
        var executor = _executorFactory.GetInstance();
        var result = await executor.BeginAsync(request.WorkflowId, request.ActionConfiguration!, request.Inputs!);
        return Ok(result);
    }


    [HttpPost("[action]")]
    public async Task<ActionResult<ExecuteTaskResponse>> Execute([FromBody] ExecuteTaskRequest request)
    {
        var executor = _executorFactory.GetInstance(request.TaskExecutorId);
        var result = await executor.ExecuteAsync();
        return Ok(result);
    }
}