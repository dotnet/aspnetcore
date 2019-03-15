// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ProjectTemplates.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class WorkerTemplateTest
    {
        public WorkerTemplateTest(ProjectFactoryFixture factoryFixture, ITestOutputHelper output)
        {
            Project = factoryFixture.CreateProject(output);
        }

        public Project Project { get; }

        [Fact(Skip = "Microsoft.NET.Sdk.Worker isn't available yet")]
        public void WorkerTemplate()
        {
            Project.RunDotNetNew("worker");
            Project.BuildProject();
        }
    }
}
