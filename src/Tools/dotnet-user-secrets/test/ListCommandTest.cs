// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests;

public class ListCommandTest
{
    private readonly ITestOutputHelper _output;

    public ListCommandTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void List_Json_OutputIsProperlyFormatted()
    {
        var secretStore = new TestSecretsStore(_output);
        secretStore.Set("key1", "value1");
        var testConsole = new TestConsole(_output);
        var reporter = new ConsoleReporter(testConsole);
        var command = new ListCommand(jsonOutput: true);

        command.Execute(new CommandContext(secretStore, reporter, testConsole));

        var output = testConsole.GetOutput();
        var jsonContent = ExtractJsonContent(output);

        Assert.Equal("{\n  \"key1\": \"value1\"\n}", jsonContent, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void List_Json_EscapesNonAsciiCharacters()
    {
        var secretStore = new TestSecretsStore(_output);
        secretStore.Set("AzureAd:ClientSecret", "abcdéƒ©˙î");
        var testConsole = new TestConsole(_output);
        var reporter = new ConsoleReporter(testConsole);
        var command = new ListCommand(jsonOutput: true);

        command.Execute(new CommandContext(secretStore, reporter, testConsole));

        var output = testConsole.GetOutput();
        var jsonContent = ExtractJsonContent(output);

        // Non-ASCII characters are Unicode-escaped using the default System.Text.Json encoding
        Assert.Equal("{\n  \"AzureAd:ClientSecret\": \"abcd\\u00E9\\u0192\\u00A9\\u02D9\\u00EE\"\n}", jsonContent, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void List_Json_HasBeginAndEndMarkers()
    {
        var secretStore = new TestSecretsStore(_output);
        secretStore.Set("key1", "value1");
        var testConsole = new TestConsole(_output);
        var reporter = new ConsoleReporter(testConsole);
        var command = new ListCommand(jsonOutput: true);

        command.Execute(new CommandContext(secretStore, reporter, testConsole));

        var output = testConsole.GetOutput();

        Assert.Contains("//BEGIN", output);
        Assert.Contains("//END", output);
        var beginIndex = output.IndexOf("//BEGIN", StringComparison.Ordinal);
        var endIndex = output.IndexOf("//END", StringComparison.Ordinal);
        Assert.True(beginIndex < endIndex, "//BEGIN should appear before //END");
    }

    [Fact]
    public void List_NonJson_OutputIsProperlyFormatted()
    {
        var secretStore = new TestSecretsStore(_output);
        secretStore.Set("key1", "value1");
        secretStore.Set("AzureAd:ClientSecret", "someSecret");
        var testConsole = new TestConsole(_output);
        var reporter = new ConsoleReporter(testConsole);
        var command = new ListCommand(jsonOutput: false);

        command.Execute(new CommandContext(secretStore, reporter, testConsole));

        var output = testConsole.GetOutput();
        Assert.Contains("key1 = value1", output);
        Assert.Contains("AzureAd:ClientSecret = someSecret", output);
    }

    [Fact]
    public void List_NonJson_EmptyStore()
    {
        var secretStore = new TestSecretsStore(_output);
        var testConsole = new TestConsole(_output);
        var reporter = new ConsoleReporter(testConsole);
        var command = new ListCommand(jsonOutput: false);

        command.Execute(new CommandContext(secretStore, reporter, testConsole));

        Assert.Contains(Resources.Error_No_Secrets_Found, testConsole.GetOutput());
    }

    [Fact]
    public void List_Json_EmptyStore()
    {
        var secretStore = new TestSecretsStore(_output);
        var testConsole = new TestConsole(_output);
        var reporter = new ConsoleReporter(testConsole);
        var command = new ListCommand(jsonOutput: true);

        command.Execute(new CommandContext(secretStore, reporter, testConsole));

        var output = testConsole.GetOutput();
        var jsonContent = ExtractJsonContent(output);

        Assert.Equal("{}", jsonContent, ignoreLineEndingDifferences: true);
    }

    private static string ExtractJsonContent(string output)
    {
        const string beginMarker = "//BEGIN";
        const string endMarker = "//END";

        var beginMarkerIndex = output.IndexOf(beginMarker, StringComparison.Ordinal);
        var endMarkerIndex = output.IndexOf(endMarker, StringComparison.Ordinal);

        Assert.True(
            beginMarkerIndex >= 0,
            $"Expected output to contain '{beginMarker}' marker, but it was not found.{Environment.NewLine}Actual output:{Environment.NewLine}{output}");

        Assert.True(
            endMarkerIndex >= 0,
            $"Expected output to contain '{endMarker}' marker, but it was not found.{Environment.NewLine}Actual output:{Environment.NewLine}{output}");

        Assert.True(
            beginMarkerIndex < endMarkerIndex,
            $"Expected '{beginMarker}' marker to appear before '{endMarker}' marker.{Environment.NewLine}Actual output:{Environment.NewLine}{output}");

        var contentStartIndex = beginMarkerIndex + beginMarker.Length;
        var contentEndIndex = endMarkerIndex;

        return output[contentStartIndex..contentEndIndex].Trim();
    }

    private sealed class TestSecretsStore : SecretsStore
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
