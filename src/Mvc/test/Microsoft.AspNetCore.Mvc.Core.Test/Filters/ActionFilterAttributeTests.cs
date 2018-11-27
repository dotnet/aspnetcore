// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    public class ActionFilterAttributeTests
    {
        [Fact]
        public async Task ActionFilterAttribute_ActionFilter_SettingResult_ShortCircuits()
        {
            await CommonFilterTest.ActionFilter_SettingResult_ShortCircuits(new Mock<ActionFilterAttribute>());
        }

        [Fact]
        public async Task ActionAttributeFilter_ActionFilter_Calls_OnActionExecuted()
        {
            await CommonFilterTest.ActionFilter_Calls_OnActionExecuted(new Mock<ActionFilterAttribute>());
        }

        [Fact]
        public async Task ActionAttributeFilter_ResultFilter_Calls_OnResultExecuted()
        {
            await CommonFilterTest.ResultFilter_Calls_OnResultExecuted(new Mock<ActionFilterAttribute>());
        }

        [Fact]
        public async Task ActionFilterAttribute_ResultFilter_SettingResult_DoesNotShortCircuit()
        {
            await CommonFilterTest.ResultFilter_SettingResult_DoesNotShortCircuit(new Mock<ActionFilterAttribute>());
        }

        [Fact]
        public async Task ActionFilterAttribute_ResultFilter_SettingCancel_ShortCircuits()
        {
            await CommonFilterTest.ResultFilter_SettingCancel_ShortCircuits(new Mock<ActionFilterAttribute>());
        }
    }
}
