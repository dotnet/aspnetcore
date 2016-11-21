// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests
{

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
            var secretStore = new TestSecretsStore();
            var command = new SetCommand.FromStdInStrategy();

            command.Execute(new CommandContext(secretStore, NullLogger.Instance, testConsole));

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
            var secretStore = new TestSecretsStore();
            var command = new SetCommand.FromStdInStrategy();

            command.Execute(new CommandContext(secretStore, NullLogger.Instance, testConsole));

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
            var options = CommandLineOptions.Parse(new [] { "set", "key", "value" }, testConsole);
            Assert.IsType<SetCommand.ForOneValueStrategy>(options.Command);
        }

        private class TestSecretsStore : SecretsStore
        {
            public TestSecretsStore()
                : base("xyz", NullLogger.Instance)
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

    public class NullLogger : ILogger
    {
        public static NullLogger Instance = new NullLogger();

        private class NullScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
        public IDisposable BeginScope<TState>(TState state)
            => new NullScope();

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }
}
