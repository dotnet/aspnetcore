// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using Microsoft.AspNetCore.Testing.xunit;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCoreModule.Test
{
    public class FunctionalTest : FunctionalTestHelper, IClassFixture<InitializeTestMachine>
    {
        private const string ANCMTestCondition = TestFlags.SkipTest;
        //private const string ANCMTestCondition = TestFlags.RunAsAdministrator;

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        public async void BasicTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            await DoBasicTest(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 5)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 5)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 1)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 0)]
        public Task RapidFailsPerMinuteTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfRapidFailsPerMinute)
        {
            return DoRapidFailsPerMinuteTest(appPoolBitness, valueOfRapidFailsPerMinute);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 25, 19, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 25, 19, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 25, 19, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 25, 19, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 5, 4, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 5, 4, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 5, 4, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 5, 4, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 0, 0, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 0, 0, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 0, 0, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 0, 0, true)]
        public Task ShutdownTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfshutdownTimeLimit, int expectedClosingTime, bool isGraceFullShutdownEnabled)
        {
            return DoShutdownTimeLimitTest(appPoolBitness, valueOfshutdownTimeLimit, expectedClosingTime, isGraceFullShutdownEnabled);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 10)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 10)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 1)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 1)]
        public Task StartupTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int starupTimeLimit)
        {
            return DoStartupTimeLimitTest(appPoolBitness, starupTimeLimit);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterBackendProcessBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterBackendProcessBeingKilled(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterW3WPProcessBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterW3WPProcessBeingKilled(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterWebConfigUpdated(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterWebConfigUpdated(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationWithURLRewrite(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleParentApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleParentApplicationWithURLRewrite(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("ANCMTestBar", "bar", "bar", IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "NA", "Microsoft.AspNetCore.Server.IISIntegration", IISConfigUtility.AppPoolBitness.noChange)]
        [InlineData("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "newValue", "newValue", IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData("ASPNETCORE_IIS_HTTPAUTH", "anonymous;", "anonymous;", IISConfigUtility.AppPoolBitness.noChange)]
        [InlineData("ASPNETCORE_IIS_HTTPAUTH", "basic;anonymous;", "basic;anonymous;", IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData("ASPNETCORE_IIS_HTTPAUTH", "windows;anonymous;", "windows;anonymous;", IISConfigUtility.AppPoolBitness.noChange)]
        [InlineData("ASPNETCORE_IIS_HTTPAUTH", "windows;basic;anonymous;", "windows;basic;anonymous;", IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData("ASPNETCORE_IIS_HTTPAUTH", "ignoredValue", "anonymous;", IISConfigUtility.AppPoolBitness.noChange)]
        public Task EnvironmentVariablesTest(string environmentVariableName, string environmentVariableValue, string expectedEnvironmentVariableValue, IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoEnvironmentVariablesTest(environmentVariableName, environmentVariableValue, expectedEnvironmentVariableValue, appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task AppOfflineTestWithRenaming(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoAppOfflineTestWithRenaming(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task AppOfflineTestWithUrlRewriteAndDeleting(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoAppOfflineTestWithUrlRewriteAndDeleting(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "a")]
        public Task PostMethodTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            return DoPostMethodTest(appPoolBitness, testData);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task DisableStartUpErrorPageTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoDisableStartUpErrorPageTest(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 10)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 2)]
        public Task ProcessesPerApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfProcessesPerApplication)
        {
            return DoProcessesPerApplicationTest(appPoolBitness, valueOfProcessesPerApplication);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "00:02:00")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "00:02:00")]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "00:01:00")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "00:01:00")]
        public Task RequestTimeoutTest(IISConfigUtility.AppPoolBitness appPoolBitness, string requestTimeout)
        {
            return DoRequestTimeoutTest(appPoolBitness, requestTimeout);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task StdoutLogEnabledTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoStdoutLogEnabledTest(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "dotnet.exe", "./")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "dotnet", @".\")]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "$env", "")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "$env", "")]
        public Task ProcessPathAndArgumentsTest(IISConfigUtility.AppPoolBitness appPoolBitness, string processPath, string argumentsPrefix)
        {
            return DoProcessPathAndArgumentsTest(appPoolBitness, processPath, argumentsPrefix);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, false)]
        public Task ForwardWindowsAuthTokenTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool enabledForwardWindowsAuthToken)
        {
            return DoForwardWindowsAuthTokenTest(appPoolBitness, enabledForwardWindowsAuthToken);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, true, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, false, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, true, false)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, false, true)]
        public Task CompressionTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool useCompressionMiddleWare, bool enableIISCompression)
        {
            return DoCompressionTest(appPoolBitness, useCompressionMiddleWare, enableIISCompression);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task CachingTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoCachingTest(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task SendHTTPSRequestTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoSendHTTPSRequestTest(appPoolBitness);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "MS-ASPNETCORE", "f")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "mS-ASPNETCORE", "fo")]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "MS-ASPNETCORE-", "foo")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "mS-ASPNETCORE-f", "fooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo")]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "MS-ASPNETCORE-foo", "foo")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "MS-ASPNETCORE-foooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo", "bar")]
        public Task FilterOutMSRequestHeadersTest(IISConfigUtility.AppPoolBitness appPoolBitness, string requestHeader, string requestHeaderValue)
        {
            return DoFilterOutMSRequestHeadersTest(appPoolBitness, requestHeader, requestHeaderValue);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, true)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, false)]
        public Task ClientCertificateMappingTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool useHTTPSMiddleWare)
        {
            return DoClientCertificateMappingTest(appPoolBitness, useHTTPSMiddleWare);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, false, DoAppVerifierTest_StartUpMode.UseGracefulShutdown, DoAppVerifierTest_ShutDownMode.RecycleAppPool, 1)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, false, DoAppVerifierTest_StartUpMode.DontUseGracefulShutdown, DoAppVerifierTest_ShutDownMode.RecycleAppPool, 1)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, false, DoAppVerifierTest_StartUpMode.UseGracefulShutdown, DoAppVerifierTest_ShutDownMode.StopAndStartAppPool, 1)]
        public Task AppVerifierTest(IISConfigUtility.AppPoolBitness appPoolBitness, bool shutdownTimeout, DoAppVerifierTest_StartUpMode startUpMode, DoAppVerifierTest_ShutDownMode shutDownMode, int repeatCount)
        {
            return DoAppVerifierTest(appPoolBitness, shutdownTimeout, startUpMode, shutDownMode, repeatCount);
        }

        //////////////////////////////////////////////////////////
        // NOTE: below test scenarios are not valid for Win7 OS
        //////////////////////////////////////////////////////////

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task WebSocketErrorhandlingTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoWebSocketErrorhandlingTest(appPoolBitness);
        }

        //////////////////////////////////////////////////////////
        // NOTE: below test scenarios are not valid for Win7 OS
        //////////////////////////////////////////////////////////
        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "IIS does not support Websocket on Win7")]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "abcdefghijklmnopqrstuvwxyz0123456789")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "a")]
        // Test reliablitiy issue with lenghty data; disabled until the reason of the test issue is figured out
        //[InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789")]
        public Task WebSocketTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            return DoWebSocketTest(appPoolBitness, testData);
        }

        [ConditionalTheory]
        [ANCMTestFlags(ANCMTestCondition)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "WAS does not handle private memory limitation with Job object on Win7")]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecylingAppPoolTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecylingAppPoolTest(appPoolBitness);
        }
    } 
}