// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests;

public class SetCommandTest
{
    private readonly ITestOutputHelper _output;

    public SetCommandTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SetsFromPipedInput()
    {
        var input = @"
{
   ""Key1"": ""str value"",
""Key2"": 1234,
""Key3"": false
}";
        var testConsole = new TestConsole(_output)
        {
            IsInputRedirected = true,
            In = new StringReader(input)
        };
        var secretStore = new TestSecretsStore(_output);
        var command = new SetCommand.FromStdInStrategy();

        command.Execute(new CommandContext(secretStore, new TestReporter(_output), testConsole));

        Assert.Equal(3, secretStore.Count);
        Assert.Equal("str value", secretStore["Key1"]);
        Assert.Equal("1234", secretStore["Key2"]);
        Assert.Equal("False", secretStore["Key3"]);
    }

    [Fact]
    public void ParsesNestedObjects()
    {
        var input = @"
                {
                   ""Key1"": {
                       ""nested"" : ""value""
                   },
                   ""array"": [ 1, 2 ]
                }";

        var testConsole = new TestConsole(_output)
        {
            IsInputRedirected = true,
            In = new StringReader(input)
        };
        var secretStore = new TestSecretsStore(_output);
        var command = new SetCommand.FromStdInStrategy();

        command.Execute(new CommandContext(secretStore, new TestReporter(_output), testConsole));

        Assert.Equal(3, secretStore.Count);
        Assert.True(secretStore.ContainsKey("Key1:nested"));
        Assert.Equal("value", secretStore["Key1:nested"]);
        Assert.Equal("1", secretStore["array:0"]);
        Assert.Equal("2", secretStore["array:1"]);
    }

    [Fact]
    public void OnlyPipesInIfNoArgs()
    {
        var testConsole = new TestConsole(_output)
        {
            IsInputRedirected = true,
            In = new StringReader("")
        };
        var options = CommandLineOptions.Parse(new[] { "set", "key", "value" }, testConsole);
        Assert.IsType<SetCommand.ForOneValueStrategy>(options.Command);
    }

    private class TestSecretsStore : SecretsStore
    {
        public TestSecretsStore(ITestOutputHelper output)
            : base("xyz", new TestReporter(output))
        {
        }

        protected override IDictionary<string, string> Load(string userSecretsId)
        {
            return new Dictionary<string, string>();
        }

        public override void Save()
        {
            // noop
        }
    }
}
