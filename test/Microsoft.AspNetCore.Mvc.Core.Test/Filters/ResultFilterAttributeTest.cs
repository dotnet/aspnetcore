// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    // These tests share code with the ActionFilterAttribute tests because the IAsyncResultFilter
    // implementations need to behave the same way.
    public class ResultFilterAttributeTest
    {
        [Fact]
        public async Task ResultFilterAttribute_ResultFilter_Calls_OnResultExecuted()
        {
            await CommonFilterTest.ResultFilter_Calls_OnResultExecuted(
                new Mock<ResultFilterAttribute>());
        }

        [Fact]
        public async Task ResultFilterAttribute_ResultFilter_SettingResult_DoesNotShortCircuit()
        {
            await CommonFilterTest.ResultFilter_SettingResult_DoesNotShortCircuit(
                new Mock<ResultFilterAttribute>());
        }

        [Fact]
        public async Task ResultFilterAttribute_ResultFilter_SettingCancel_ShortCircuits()
        {
            await CommonFilterTest.ResultFilter_SettingCancel_ShortCircuits(
                new Mock<ResultFilterAttribute>());
        }
    }
}

