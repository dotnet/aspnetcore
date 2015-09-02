// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ActionResults;
using Microsoft.AspNet.Mvc.Filters;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class SaveTempDataFilterTest
    {
        [Fact]
        public void SaveTempDataFilter_OnResourceExecuted_SavesTempData()
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>(MockBehavior.Strict);
            tempData
                .Setup(m => m.Save())
                .Verifiable();

            var filter = new SaveTempDataFilter(tempData.Object);

            var context = new ResourceExecutedContext(new ActionContext(), new IFilterMetadata[] { });

            // Act
            filter.OnResourceExecuted(context);

            // Assert
            tempData.Verify();
        }

        [Fact]
        public void SaveTempDataFilter_OnResultExecuted_KeepsTempData_ForIKeepTempDataResult()
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>(MockBehavior.Strict);
            tempData
                .Setup(m => m.Keep())
                .Verifiable();

            var filter = new SaveTempDataFilter(tempData.Object);

            var context = new ResultExecutedContext(
                new ActionContext(),
                new IFilterMetadata[] { },
                new Mock<IKeepTempDataResult>().Object,
                new object());

            // Act
            filter.OnResultExecuted(context);

            // Assert
            tempData.Verify();
        }

        [Fact]
        public void SaveTempDataFilter_OnResultExecuted_DoesNotKeepTempData_ForNonIKeepTempDataResult()
        {
            // Arrange
            var tempData = new Mock<ITempDataDictionary>(MockBehavior.Strict);
            var filter = new SaveTempDataFilter(tempData.Object);

            var context = new ResultExecutedContext(
                new ActionContext(),
                new IFilterMetadata[] { },
                new Mock<IActionResult>().Object,
                new object());

            // Act 
            filter.OnResultExecuted(context);

            // Assert - The mock will throw if we do the wrong thing.
        }
    }
}
