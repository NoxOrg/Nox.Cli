using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nox.Cli.Shared.DTO.Enumerations;
using Nox.Cli.Shared.DTO.Workflow;

namespace Nox.Cli.Server.Controllers;

[Authorize]
[Route("[controller]/v1/")]
[Produces("application/json")]
public class WorkflowController : Controller
{
    [HttpPost("[action]")]
    public ActionResult<ExecuteWorkflowResponse> Execute([FromBody] ExecuteWorkflowRequest request)
    {
        var result = new ExecuteWorkflowResponse
        {
            WorkflowId = request.WorkflowId,
            State = WorkflowExecutionState.Accepted
        };
        //Todo start executing the workflow
        return Ok(result);
    }
}