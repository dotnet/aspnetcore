// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities
{
    public class LogFileTestBase : IISFunctionalTestBase
    {
        protected string _logFolderPath;

        public LogFileTestBase(ITestOutputHelper output = null) : base(output)
        {
            _logFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }
        public override void Dispose()
        {
            base.Dispose();
            if (Directory.Exists(_logFolderPath))
            {
                Directory.Delete(_logFolderPath, true);
            }
        }

        public string GetLogFileContent(IISDeploymentResult deploymentResult)
        {
            return Helpers.ReadAllTextFromFile(Helpers.GetExpectedLogName(deploymentResult, _logFolderPath), Logger);
        }
    }
}
