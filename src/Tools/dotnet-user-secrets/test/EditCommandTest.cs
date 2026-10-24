// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.UserSecrets.Tests;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests;

public class EditCommandTest(UserSecretsTestFixture fixture, ITestOutputHelper output) : IClassFixture<UserSecretsTestFixture>
{
    [Fact]
    public void UsesPlatformDefaultEditor()
    {
        fixture.GetTempSecretProject(out var userSecretsId);
        var secretsFile = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var reporter = new TestReporter(output);
        var console = new TestConsole(output);
        var secretsStore = new SecretsStore(userSecretsId, reporter, true);
        var context = new CommandContext(secretsStore, reporter, console);

        var actualEditorCalls = new List<ValueTuple<string, List<string>>>();
        var command = new EditCommand
        {
            _getEnvironmentVariable = _ => null,
            _runProcess = (editor, editorArgs) => actualEditorCalls.Add(ValueTuple.Create(editor, editorArgs))
        };

        var (platformDefaultEditor, platformDefaultEditorArgs) = EditCommand.GetPlatformDefaultEditorAndArgs();
        Assert.False(string.IsNullOrEmpty(platformDefaultEditor));
        command.Execute(context);
        Assert.True(File.Exists(secretsFile));
        Assert.Equal(new[] { (platformDefaultEditor, platformDefaultEditorArgs.Concat([secretsFile]).ToList()) }, actualEditorCalls);
    }

    [Fact]
    public void UsesEditorSpecifiedByEnvironmentVariables()
    {
        fixture.GetTempSecretProject(out var userSecretsId);
        var secretsFile = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        var reporter = new TestReporter(output);
        var console = new TestConsole(output);
        var secretsStore = new SecretsStore(userSecretsId, reporter, true);
        var context = new CommandContext(secretsStore, reporter, console);

        var fakeEnvironmentVariables = new Dictionary<string, string>();
        var actualEditorCalls = new List<ValueTuple<string, List<string>>>();
        var command = new EditCommand
        {
            _getEnvironmentVariable = fakeEnvironmentVariables.GetValueOrDefault,
            _runProcess = (editor, editorArgs) => actualEditorCalls.Add(ValueTuple.Create(editor, editorArgs))
        };

        // EDITOR should be used if set
        fakeEnvironmentVariables["EDITOR"] = "my-editor-1";
        command.Execute(context);
        Assert.True(File.Exists(secretsFile));
        Assert.Equal(new[] { ("my-editor-1", new List<string> { secretsFile }) }, actualEditorCalls);
        actualEditorCalls.Clear();

        // VISUAL should be used if set, even if EDITOR is set.
        fakeEnvironmentVariables["VISUAL"] = "my-editor-2";
        command.Execute(context);
        Assert.True(File.Exists(secretsFile));
        Assert.Equal(new[] { ("my-editor-2", new List<string> { secretsFile }) }, actualEditorCalls);
        actualEditorCalls.Clear();

        // DOTNET_USER_SECRETS_EDITOR should be used if set, even if VISUAL and EDITOR are set.
        fakeEnvironmentVariables["DOTNET_USER_SECRETS_EDITOR"] = "my-editor-3";
        command.Execute(context);
        Assert.True(File.Exists(secretsFile));
        Assert.Equal(new[] { ("my-editor-3", new List<string> { secretsFile }) }, actualEditorCalls);
    }

    [Fact]
    public void CanEditMalformedSecretsFile()
    {
        fixture.GetTempSecretProject(out var userSecretsId);
        var secretsFile = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsFile)!);
        File.WriteAllText(secretsFile, "{"); // Intentionally malformed JSON to test that the editor is still launched.
        var reporter = new TestReporter(output);
        var console = new TestConsole(output);
        var secretsStore = new SecretsStore(userSecretsId, reporter, true);
        var context = new CommandContext(secretsStore, reporter, console);

        var actualEditorCalls = new List<ValueTuple<string, List<string>>>();
        var command = new EditCommand
        {
            _getEnvironmentVariable = _ => null,
            _runProcess = (editor, editorArgs) => actualEditorCalls.Add(ValueTuple.Create(editor, editorArgs))
        };

        var (platformDefaultEditor, platformDefaultEditorArgs) = EditCommand.GetPlatformDefaultEditorAndArgs();
        Assert.False(string.IsNullOrEmpty(platformDefaultEditor));
        command.Execute(context);
        Assert.True(File.Exists(secretsFile));
        Assert.Equal(new[] { (platformDefaultEditor, platformDefaultEditorArgs.Concat([secretsFile]).ToList()) }, actualEditorCalls);
    }
}
