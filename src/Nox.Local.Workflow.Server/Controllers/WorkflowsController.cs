using Microsoft.AspNetCore.Mvc;
using Nox.Cli.Configuration;

namespace Nox.Local.Workflow.Server.Controllers;

[ApiController]
[Route("88155c28-f750-4013-91d3-8347ddb3daa7")]
public class WorkflowsController : ControllerBase
{
    public IEnumerable<RemoteFileInfo> Get()
    {
        var result = new List<RemoteFileInfo>();
        foreach (var file in Directory.GetFiles("./workflow"))
        {
            var fileInfo = new FileInfo(file);
            result.Add(new RemoteFileInfo
            {
                Name = Path.GetFileName(file),
                Size = (int)fileInfo.Length,
                ShaChecksum = "ABCD"
            });
        }

        return result;
    }

    [HttpGet("{filename}")]
    [Produces("text/plain")]
    public string Get(string filename)
    {
        var result = System.IO.File.ReadAllText($"./workflow/{filename}");
        return result;
    }
}