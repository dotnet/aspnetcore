// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelperPartialExtensionsTest
    {
        public static TheoryData<Func<IHtmlHelper, HtmlString>> PartialExtensionMethods
        {
            get
            {
                var vdd = new ViewDataDictionary(new EmptyModelMetadataProvider());
                return new TheoryData<Func<IHtmlHelper, HtmlString>>
                {
                    helper => helper.Partial("test"),
                    helper => helper.Partial("test", new object()),
                    helper => helper.Partial("test", vdd),
                    helper => helper.Partial("test", new object(), vdd)
                };
            }
        }

        [Theory]
        [MemberData(nameof(PartialExtensionMethods))]
        public void PartialMethods_DoesNotWrapThrownException(Func<IHtmlHelper, HtmlString> partialMethod)
        {
            // Arrange
            var expected = new InvalidOperationException();
            var helper = new Mock<IHtmlHelper>();
            helper.Setup(h => h.PartialAsync("test", It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
                  .Callback(() =>
                  {
                      // Workaround for compilation issue with Moq.
                      helper.ToString();
                      throw expected;
                  });
            helper.SetupGet(h => h.ViewData)
                  .Returns(new ViewDataDictionary(new EmptyModelMetadataProvider()));

            // Act and Assert
            var actual = Assert.Throws<InvalidOperationException>(() => partialMethod(helper.Object));
            Assert.Same(expected, actual);
        }

        [Fact]
        public void Partial_InvokesPartialAsyncWithCurrentModel()
        {
            // Arrange
            var expected = new HtmlString("value");
            var model = new object();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
            {
                Model = model
            };
            var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
            helper.Setup(h => h.PartialAsync("test", model, null))
                  .Returns(Task.FromResult(expected))
                  .Verifiable();
            helper.SetupGet(h => h.ViewData)
                  .Returns(viewData);

            // Act
            var actual = helper.Object.Partial("test");

            // Assert
            Assert.Same(expected, actual);
            helper.Verify();
        }

        [Fact]
        public void PartialWithModel_InvokesPartialAsyncWithPassedInModel()
        {
            // Arrange
            var expected = new HtmlString("value");
            var model = new object();
            var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
            helper.Setup(h => h.PartialAsync("test", model, null))
                  .Returns(Task.FromResult(expected))
                  .Verifiable();

            // Act
            var actual = helper.Object.Partial("test", model);

            // Assert
            Assert.Same(expected, actual);
            helper.Verify();
        }

        [Fact]
        public void PartialWithViewData_InvokesPartialAsyncWithPassedInViewData()
        {
            // Arrange
            var expected = new HtmlString("value");
            var model = new object();
            var passedInViewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
            {
                Model = model
            };
            var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
            helper.Setup(h => h.PartialAsync("test", model, passedInViewData))
                  .Returns(Task.FromResult(expected))
                  .Verifiable();
            helper.SetupGet(h => h.ViewData)
                  .Returns(viewData);

            // Act
            var actual = helper.Object.Partial("test", passedInViewData);

            // Assert
            Assert.Same(expected, actual);
            helper.Verify();
        }

        [Fact]
        public void PartialWithViewDataAndModel_InvokesPartialAsyncWithPassedInViewDataAndModel()
        {
            // Arrange
            var expected = new HtmlString("value");
            var passedInModel = new object();
            var passedInViewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
            helper.Setup(h => h.PartialAsync("test", passedInModel, passedInViewData))
                  .Returns(Task.FromResult(expected))
                  .Verifiable();

            // Act
            var actual = helper.Object.Partial("test", passedInModel, passedInViewData);

            // Assert
            Assert.Same(expected, actual);
            helper.Verify();
        }

        [Fact]
        public void Partial_InvokesAndRendersPartialAsyncOnHtmlHelperOfT()
        {
            // Arrange
            var model = new TestModel();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var expected = DefaultTemplatesUtilities.FormatOutput(helper, model);

            // Act
            var actual = helper.Partial("some-partial");

            // Assert
            Assert.Equal(expected, actual.ToString());
        }

        [Fact]
        public void PartialWithModel_InvokesAndRendersPartialAsyncOnHtmlHelperOfT()
        {
            // Arrange
            var model = new TestModel();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            var expected = DefaultTemplatesUtilities.FormatOutput(helper, model);

            // Act
            var actual = helper.Partial("some-partial", model);

            // Assert
            Assert.Equal(expected, actual.ToString());
        }

        [Fact]
        public void PartialWithViewData_InvokesAndRendersPartialAsyncOnHtmlHelperOfT()
        {
            // Arrange
            var model = new TestModel();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var viewData = new ViewDataDictionary(helper.MetadataProvider);
            var expected = DefaultTemplatesUtilities.FormatOutput(helper, model);

            // Act
            var actual = helper.Partial("some-partial", viewData);

            // Assert
            Assert.Equal(expected, actual.ToString());
        }

        private sealed class TestModel
        {
            public override string ToString()
            {
                return "test-model-content";
            }
        }
    }
}