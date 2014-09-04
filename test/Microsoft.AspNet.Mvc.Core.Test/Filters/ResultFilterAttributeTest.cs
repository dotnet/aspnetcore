// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    // These tests share code with the ActionFilterAttribute tests because the IAsyncResultFilter
    // implementations need to behave the same way.
    public class ResultFilterAttributeTest
    {
        [Fact]
        public async Task ResultFilterAttribute_ResultFilter_Calls_OnResultExecuted()
        {
            await ActionFilterAttributeTests.ResultFilter_Calls_OnResultExecuted(
                new Mock<ResultFilterAttribute>());
        }

        [Fact]
        public async Task ResultFilterAttribute_ResultFilter_SettingResult_DoesNotShortCircuit()
        {
            await ActionFilterAttributeTests.ResultFilter_SettingResult_DoesNotShortCircuit(
                new Mock<ResultFilterAttribute>());
        }

        [Fact]
        public async Task ResultFilterAttribute_ResultFilter_SettingCancel_ShortCircuits()
        {
            await ActionFilterAttributeTests.ResultFilter_SettingCancel_ShortCircuits(
                new Mock<ResultFilterAttribute>());
        }
    }
}
#endif
