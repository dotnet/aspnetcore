// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using Microsoft.AspNetCore.Testing.xunit;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCoreModule.Test
{
    public class FunctionalTest : FunctionalTestHelper, IClassFixture<InitializeTestMachine>
    {
        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, IISConfigUtility.AppPoolBitness.noChange)]
        [InlineData(ServerType.IISExpress, IISConfigUtility.AppPoolBitness.enable32Bit)]
        public Task BasicTestOnIISExpress(ServerType serverType, IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoBasicTest(serverType, appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IIS, IISConfigUtility.AppPoolBitness.noChange)]
        [InlineData(ServerType.IIS, IISConfigUtility.AppPoolBitness.enable32Bit)]
        public Task BasicTestOnIIS(ServerType serverType, IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoBasicTest(serverType, appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
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

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 25, 19)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 25, 19)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 5, 4)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 5, 4)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 0, 0)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 0, 0)]
        public Task ShutdownTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfshutdownTimeLimit, int expectedClosingTime)
        {
            return DoShutdownTimeLimitTest(appPoolBitness, valueOfshutdownTimeLimit, expectedClosingTime);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
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

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "a")]
        public Task WebSocketTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            return DoWebSocketTest(appPoolBitness, testData);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterBackendProcessBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterBackendProcessBeingKilled(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterW3WPProcessBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterW3WPProcessBeingKilled(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterWebConfigUpdated(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterWebConfigUpdated(appPoolBitness);
        }
        
        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationWithURLRewrite(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleParentApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleParentApplicationWithURLRewrite(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task EnvironmentVariablesTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoEnvironmentVariablesTest(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task AppOfflineTestWithRenaming(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoAppOfflineTestWithRenaming(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task AppOfflineTestWithUrlRewriteAndDeleting(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoAppOfflineTestWithUrlRewriteAndDeleting(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "a")]
        public Task PostMethodTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            return DoPostMethodTest(appPoolBitness, testData);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task DisableStartUpErrorPageTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoDisableStartUpErrorPageTest(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 10)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 2)]
        public Task ProcessesPerApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfProcessesPerApplication)
        {
            return DoProcessesPerApplicationTest(appPoolBitness, valueOfProcessesPerApplication);
        }
        
        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
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
        
        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task StdoutLogEnabledTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoStdoutLogEnabledTest(appPoolBitness);
        }
        
        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
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

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
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

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecylingAppPoolTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecylingAppPoolTest(appPoolBitness);
        }

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
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

        [EnvironmentVariableTestCondition("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task CachingTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoCachingTest(appPoolBitness);
        }
    }
}
