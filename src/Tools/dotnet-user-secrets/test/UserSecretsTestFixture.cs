// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Microsoft.Extensions.Configuration.UserSecrets.Tests;

public class UserSecretsTestFixture : IDisposable
{
    private Stack<Action> _disposables = new Stack<Action>();

    public const string TestSecretsId = "b918174fa80346bbb7f4a386729c0eff";

    public UserSecretsTestFixture()
    {
        _disposables.Push(() => TryDelete(Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(TestSecretsId))));
    }

    public void Dispose()
    {
        while (_disposables.Count > 0)
        {
            _disposables.Pop()?.Invoke();
        }
    }

    public string GetTempSecretProject()
    {
        string userSecretsId;
        return GetTempSecretProject(out userSecretsId);
    }

    private const string ProjectTemplate = @"<Project ToolsVersion=""15.0"" Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    {0}
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
  </ItemGroup>
</Project>";

    public string GetTempSecretProject(out string userSecretsId)
    {
        userSecretsId = Guid.NewGuid().ToString();
        return CreateProject(userSecretsId);
    }

    public string CreateProject(string userSecretsId)
    {
        var projectPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "usersecretstest", Guid.NewGuid().ToString()));
        var prop = string.IsNullOrEmpty(userSecretsId)
            ? string.Empty
            : $"<UserSecretsId>{userSecretsId}</UserSecretsId>";

        File.WriteAllText(
            Path.Combine(projectPath.FullName, "TestProject.csproj"),
            string.Format(CultureInfo.InvariantCulture, ProjectTemplate, prop));

        var id = userSecretsId;
        _disposables.Push(() =>
        {
            try
            {
                // may throw if id is bad
                var secretsDir = Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(id));
                TryDelete(secretsDir);
            }
            catch { }
        });
        _disposables.Push(() => TryDelete(projectPath.FullName));

        return projectPath.FullName;
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
        catch (Exception)
        {
            // Ignore failures.
            Console.WriteLine("Failed to delete " + directory);
        }
    }
}
