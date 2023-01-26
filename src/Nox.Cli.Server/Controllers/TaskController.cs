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
    
    
    [HttpGet("[action]/{taskExecutorId}")]
    public ActionResult<ExecuteTaskResponse> GetState(Guid taskExecutorId)
    {
        var executor = _executorFactory.GetInstance(taskExecutorId);
        var result = new TaskStateResponse
        {
            TaskExecutorId = taskExecutorId,
            WorkflowId = executor.WorkflowId,
            State = executor.State,
            StateName = Enum.GetName(executor.State)
        };
        return Ok(result);
    }
    
    [HttpPost("[action]")]
    public async Task<ActionResult<ExecuteTaskResponse>> Begin([FromBody] BeginTaskRequest request)
    {
        var executor = _executorFactory.NewInstance(request.WorkflowId);
        
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