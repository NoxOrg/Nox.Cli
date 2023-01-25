using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nox.Cli.Abstractions;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Controllers;

[Authorize]
[Route("[controller]/v1/")]
[Produces("application/json")]
public class TaskController : Controller
{
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
    public ActionResult<ExecuteTaskResponse> Execute([FromBody] ExecuteTaskRequest request)
    {
        var result = new ExecuteTaskResponse
        {
            WorkflowId = request.WorkflowId,
            
        };
        //Todo start executing the workflow
        return Ok(result);
    }
}