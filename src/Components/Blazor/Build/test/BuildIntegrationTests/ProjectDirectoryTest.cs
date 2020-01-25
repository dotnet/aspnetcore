// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class ProjectDirectoryTest
    {
        [Fact]
        public void ProjectDirectory_IsNotSetToBePreserved()
        {
            // Arrange
            using var project = ProjectDirectory.Create("standalone");

            // Act & Assert
            // This flag is only meant for local debugging and should not be set when checking in.
            Assert.False(project.PreserveWorkingDirectory);
        }
    }
}
