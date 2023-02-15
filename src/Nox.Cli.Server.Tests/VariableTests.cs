using System.Text.Json;
using Nox.Cli.Variables;
using NUnit.Framework.Interfaces;

namespace Nox.Cli.Server.Tests;

public class VariableTests
{
    [Test]
    public void Can_Parse_Json_Input_Variables()
    {
        var vars = GetTestVars();
        var serializedVars = JsonSerializer.Serialize(vars);
        var deserializedVars = JsonSerializer.Deserialize<IDictionary<string, Variable>>(serializedVars);
        var result = VariableHelper.ParseJsonInputs(deserializedVars);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(4));
        Assert.That(result.ContainsKey("var.string"), Is.True);
        Assert.That(result["var.string"].Value, Is.EqualTo("Test Variable 1"));
        Assert.That(result["var.string"].IsSecret, Is.True);
        Assert.That(result.ContainsKey("var.int"), Is.True);
        Assert.That(result["var.int"].Value, Is.EqualTo(10));
        Assert.That(result.ContainsKey("var.double"), Is.True);
        Assert.That(result["var.double"].Value, Is.EqualTo(9.999));
        Assert.That(result.ContainsKey("var.datetime"), Is.True);
        Assert.That(result["var.datetime"].Value, Is.EqualTo("2023-02-15T05:52:32"));
        Assert.That(result.ContainsKey("var.person"), Is.False);
        
    }

    private IDictionary<string, Variable> GetTestVars()
    {
        var result = new Dictionary<string, Variable>();
        //string
        result.Add("var.string", new Variable("Test Variable 1", true));
        //int
        result.Add("var.int", new Variable(10));
        //double
        result.Add("var.double", new Variable(9.999));
        //DateTime
        result.Add("var.datetime", new Variable(new DateTime(2023, 02, 15, 5, 52, 32)));
        //class
        result.Add("var.person", new Variable(new TestPerson{Name = "Test User", Age = 5}));
        return result;
    }
    
    
}