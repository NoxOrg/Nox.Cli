using Nox.Cli.Abstractions.Extensions;
using NUnit.Framework;

namespace CoreTests;

public class CliAddinExtensionTests
{
    [Test]
    public void Should_be_able_to_parse_basic_inputs()
    {
        var inputs = new Dictionary<string, object>();
        inputs.Add("stringVar", "This is a string");
        inputs.Add("intVar", 1234);
        inputs.Add("decimalVar", 123.45);
        inputs.Add("trueVar", true);
        inputs.Add("falseVar", false);
        var stringVal = inputs.Value<string>("stringVar");
        Assert.That(stringVal, Is.TypeOf(typeof(string)));
        Assert.That(stringVal, Is.EqualTo("This is a string"));
        var intVal = inputs.Value<int>("intVar");
        Assert.That(intVal, Is.TypeOf(typeof(int)));
        Assert.That(intVal, Is.EqualTo(1234));
        var decimalVal = inputs.Value<double>("decimalVar");
        Assert.That(decimalVal, Is.TypeOf(typeof(double)));
        Assert.That(decimalVal, Is.EqualTo(123.45));
        var trueVal = inputs.Value<bool>("trueVar");
        Assert.That(trueVal, Is.TypeOf(typeof(bool)));
        Assert.That(trueVal, Is.EqualTo(true));
        var falseVal = inputs.Value<bool>("falseVar");
        Assert.That(falseVal, Is.TypeOf(typeof(bool)));
        Assert.That(falseVal, Is.EqualTo(false));
    }
    
    [Test]
    public void Should_be_able_to_parse_simple_dictionary_inputs()
    {
        var inputs = new Dictionary<string, object>();
        var objDict = new Dictionary<string, object>
        {
            { "val1", "Hello world" },
            { "val2", 1234 },
            { "val3", 123.45 },
            { "val4", true }
        };
        inputs.Add("objectDict", objDict);
        var stringDict = new Dictionary<string, string>
        {
            { "val1", "This is the first string" },
            { "val2", "This is the second string" }
        };
        inputs.Add("stringDict", stringDict);
        var intDict = new Dictionary<string, int>
        {
            { "val1", 123 },
            { "val2", 456 }
        };
        inputs.Add("intDict", intDict);
        var doubleDict = new Dictionary<string, double>
        {
            { "val1", 123.45 },
            { "val2", 456.78 }
        };
        inputs.Add("doubleDict", doubleDict);
        var objVal = inputs.Value<Dictionary<string, object>>("objectDict");
        Assert.That(objVal, Is.Not.Null);
        Assert.That(objVal, Is.TypeOf(typeof(Dictionary<string, object>)));
        Assert.That(objVal!, Has.Count.EqualTo(4));
        
        var stringVal = inputs.Value<Dictionary<string, string>>("stringDict");
        Assert.That(stringVal, Is.Not.Null);
        Assert.That(stringVal, Is.TypeOf(typeof(Dictionary<string, string>)));
        Assert.That(stringVal!, Has.Count.EqualTo(2));
        
        var intVal = inputs.Value<Dictionary<string, int>>("intDict");
        Assert.That(intVal, Is.Not.Null);
        Assert.That(intVal, Is.TypeOf(typeof(Dictionary<string, int>)));
        Assert.That(intVal!, Has.Count.EqualTo(2));
        
        var doubleVal = inputs.Value<Dictionary<string, double>>("doubleDict");
        Assert.That(doubleVal, Is.Not.Null);
        Assert.That(doubleVal, Is.TypeOf(typeof(Dictionary<string, double>)));
        Assert.That(doubleVal!, Has.Count.EqualTo(2));
    }
    
    [Test]
    public void Should_be_able_to_parse_complex_dictionary_inputs()
    {
        var inputs = new Dictionary<string, object>();
        var objDict = new Dictionary<object, object>
        {
            { "val1", "Hello world" },
            { "val2", 1234 },
            { "val3", 123.45 },
            { "val4", true }
        };
        inputs.Add("objectDict", objDict);
        var stringDict = new Dictionary<object, object>
        {
            { "val1", "This is the first string" },
            { "val2", "This is the second string" }
        };
        inputs.Add("stringDict", stringDict);
        var intDict = new Dictionary<object, object>
        {
            { "val1", 123 },
            { "val2", 456 }
        };
        inputs.Add("intDict", intDict);
        var doubleDict = new Dictionary<object, object>
        {
            { "val1", 123.45 },
            { "val2", 456.78 }
        };
        inputs.Add("doubleDict", doubleDict);
        
        var objVal = inputs.Value<Dictionary<string, object>>("objectDict");
        Assert.That(objVal, Is.Not.Null);
        Assert.That(objVal, Is.TypeOf(typeof(Dictionary<string, object>)));
        Assert.That(objVal!, Has.Count.EqualTo(4));
        
        var stringVal = inputs.Value<Dictionary<string, string>>("stringDict");
        Assert.That(stringVal, Is.Not.Null);
        Assert.That(stringVal, Is.TypeOf(typeof(Dictionary<string, string>)));
        Assert.That(stringVal!, Has.Count.EqualTo(2));
        
        var intVal = inputs.Value<Dictionary<string, int>>("intDict");
        Assert.That(intVal, Is.Not.Null);
        Assert.That(intVal, Is.TypeOf(typeof(Dictionary<string, int>)));
        Assert.That(intVal!, Has.Count.EqualTo(2));
        
        var doubleVal = inputs.Value<Dictionary<string, double>>("doubleDict");
        Assert.That(doubleVal, Is.Not.Null);
        Assert.That(doubleVal, Is.TypeOf(typeof(Dictionary<string, double>)));
        Assert.That(doubleVal!, Has.Count.EqualTo(2));
    }
    
    
}