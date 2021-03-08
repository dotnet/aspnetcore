// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ProjectTemplates.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class BlazorTemplateTest : LoggedTest
    {
        public BlazorTemplateTest(PlaywrightFixture<BlazorServerTemplateTest> browserFixture)
        {
            Fixture = browserFixture;
        }

        public PlaywrightFixture<BlazorServerTemplateTest> Fixture { get; }
        public ContextInformation BrowserContextInfo { get; private set; }

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper) 
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

            TestSink.MessageLogged += LogMessage;
            BrowserContextInfo = new ContextInformation(LoggerFactory);
            
            void LogMessage(WriteContext ctx)
            {
                testOutputHelper.WriteLine($"{MapLogLevel(ctx)}: [Browser]{ctx.Message}");

                static string MapLogLevel(WriteContext obj) => obj.LogLevel switch
                {
                    LogLevel.Trace => "trace",
                    LogLevel.Debug => "dbug",
                    LogLevel.Information => "info",
                    LogLevel.Warning => "warn",
                    LogLevel.Error => "error",
                    LogLevel.Critical => "crit",
                    LogLevel.None => "info",
                    _ => "info"
                };
            }
        }

        public static bool TryValidateBrowserRequired(BrowserKind browserKind, bool isRequired, out string error)
        {
            error = !isRequired ? null : $"Browser '{browserKind}' is required but not configured on '{RuntimeInformation.OSDescription}'";
            return isRequired;
        }

        protected void EnsureBrowserAvailable(BrowserKind browserKind)
        {
            Assert.False(
                TryValidateBrowserRequired(
                    browserKind,
                    isRequired: !Fixture.BrowserManager.IsExplicitlyDisabled(browserKind),
                    out var errorMessage),
                errorMessage);
        }
    }
}
