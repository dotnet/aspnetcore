// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ProjectTemplates.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class GrpcTemplateTest
    {
        public GrpcTemplateTest(ProjectFactoryFixture factoryFixture, ITestOutputHelper output)
        {
            Project = factoryFixture.CreateProject(output);
        }

        public Project Project { get; }

        [Fact]
        public void GrpcTemplate()
        {
            Project.RunDotNetNew("grpc");
            Project.BuildProject();
        }
    }
}
