// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.ProjectModel.Internal;
using Xunit;

namespace Microsoft.Extensions.ProjectModel
{
    public class DotNetCoreSdkResolverTest : IDisposable
    {
        private readonly string _fakeInstallDir;
        public DotNetCoreSdkResolverTest()
        {
            _fakeInstallDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_fakeInstallDir);
            Directory.CreateDirectory(Path.Combine(_fakeInstallDir, "sdk"));
        }

        [Fact]
        public void ResolveLatest()
        {
            var project = Path.Combine(_fakeInstallDir, "project");
            Directory.CreateDirectory(project);
            Directory.CreateDirectory(Path.Combine(_fakeInstallDir, "sdk", "1.0.1"));
            Directory.CreateDirectory(Path.Combine(_fakeInstallDir, "sdk", "1.0.0"));
            Directory.CreateDirectory(Path.Combine(_fakeInstallDir, "sdk", "1.0.0-beta1"));
            var sdk = new DotNetCoreSdkResolver(_fakeInstallDir).ResolveLatest();
            Assert.Equal("1.0.1", sdk.Version);
            Assert.Equal(Path.Combine(_fakeInstallDir, "sdk", "1.0.1"), sdk.BasePath);
        }

        [Fact]
        public void ResolveProjectSdk()
        {
            var project = Path.Combine(_fakeInstallDir, "project");
            Directory.CreateDirectory(project);
            Directory.CreateDirectory(Path.Combine(_fakeInstallDir, "sdk", "1.0.0"));
            Directory.CreateDirectory(Path.Combine(_fakeInstallDir, "sdk", "1.0.0-abc-123"));
            Directory.CreateDirectory(Path.Combine(_fakeInstallDir, "sdk", "1.0.0-xyz-123"));
            File.WriteAllText(Path.Combine(_fakeInstallDir, "global.json"), @"{
                ""sdk"": {
                    ""version"": ""1.0.0-abc-123""
                }
            }");
            var sdk = new DotNetCoreSdkResolver(_fakeInstallDir).ResolveProjectSdk(project);
            Assert.Equal("1.0.0-abc-123", sdk.Version);
            Assert.Equal(Path.Combine(_fakeInstallDir, "sdk", "1.0.0-abc-123"), sdk.BasePath);
        }

        public void Dispose()
        {
            Directory.Delete(_fakeInstallDir, recursive: true);
        }
    }
}