// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if MOCK_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class DefaultHtmlGeneratorTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetCurrentValues_WithEmptyViewData_ReturnsNull(bool allowMultiple)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Name),
                allowMultiple: allowMultiple);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetCurrentValues_WithNullExpressionResult_ReturnsNull(bool allowMultiple)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer,
                expression: nameof(Model.Name),
                allowMultiple: allowMultiple);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentValues_WithNullExpression_Throws()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

            var expected = "The name of an HTML field cannot be null or empty. Instead use methods " + 
                "Microsoft.AspNet.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNet.Mvc.Rendering." + 
                "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value." +
                Environment.NewLine + "Parameter name: expression";

            // Act and assert
            var ex = Assert.Throws<ArgumentException>(
                "expression",
                () => htmlGenerator.GetCurrentValues(
                    viewContext,
                    modelExplorer,
                    expression: null,
                    allowMultiple: true));

            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void GenerateSelect_WithNullExpression_Throws()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

            var expected = "The name of an HTML field cannot be null or empty. Instead use methods " +
                "Microsoft.AspNet.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNet.Mvc.Rendering." +
                "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value." +
                Environment.NewLine + "Parameter name: expression";

            // Act and assert
            var ex = Assert.Throws<ArgumentException>(
                "expression",
                () => htmlGenerator.GenerateSelect(
                    viewContext,
                    modelExplorer,
                    "label",
                    null,
                    new List<SelectListItem>(),
                    true,
                    null));

            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void GenerateTextArea_WithNullExpression_Throws()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

            var expected = "The name of an HTML field cannot be null or empty. Instead use methods " +
                "Microsoft.AspNet.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNet.Mvc.Rendering." +
                "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value." +
                Environment.NewLine + "Parameter name: expression";

            // Act and assert
            var ex = Assert.Throws<ArgumentException>(
                "expression", 
                () => htmlGenerator.GenerateTextArea(
                    viewContext,
                    modelExplorer,
                    null,
                    1,
                    1,
                    null));

            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void GenerateValidationMessage_WithNullExpression_Throws()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), model: null);

            var expected = "The name of an HTML field cannot be null or empty. Instead use methods " +
                "Microsoft.AspNet.Mvc.Rendering.IHtmlHelper.Editor or Microsoft.AspNet.Mvc.Rendering." +
                "IHtmlHelper`1.EditorFor with a non-empty htmlFieldName argument value." +
                Environment.NewLine + "Parameter name: expression";

            // Act and assert
            var ex = Assert.Throws<ArgumentException>(
                "expression", 
                () => htmlGenerator.GenerateValidationMessage(
                    viewContext,
                    null,
                    "Message",
                    "tag",
                    null));

            Assert.Equal(expected, ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetCurrentValues_WithSelectListInViewData_ReturnsNull(bool allowMultiple)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = Enumerable.Empty<SelectListItem>();

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Name),
                allowMultiple: allowMultiple);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("some string")] // treated as if it were not IEnumerable
        [InlineData(23)]
        [InlineData(RegularEnum.Three)]
        public void GetCurrentValues_AllowMultipleWithNonEnumerableInViewData_Throws(object value)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = value;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Name),
                allowMultiple: true));
            Assert.Equal(
                "The parameter 'expression' must evaluate to an IEnumerable when multiple selection is allowed.",
                exception.Message);
        }

        // rawValue, allowMultiple -> expected current values
        public static TheoryData<string[], bool, IReadOnlyCollection<string>> GetCurrentValues_CollectionData
        {
            get
            {
                return new TheoryData<string[], bool, IReadOnlyCollection<string>>
                {
                    // ModelStateDictionary converts arrays to single values if needed.
                    { new [] { "some string" }, false, new [] { "some string" } },
                    { new [] { "some string" }, true, new [] { "some string" } },
                    { new [] { "some string", "some other string" }, false, new [] { "some string" } },
                    {
                        new [] { "some string", "some other string" },
                        true,
                        new [] { "some string", "some other string" }
                    },
                    // { new string[] { null }, false, null } would fall back to other sources.
                    { new string[] { null }, true, new [] { string.Empty } },
                    { new [] { string.Empty }, false, new [] { string.Empty } },
                    { new [] { string.Empty }, true, new [] { string.Empty } },
                    {
                        new [] { null, "some string", "some other string" },
                        true,
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores duplicates
                    {
                        new [] { null, "some string", null, "some other string", null, "some string", null },
                        true,
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores case of duplicates
                    {
                        new [] { "some string", "SoMe StriNg", "Some String", "soME STRing", "SOME STRING" },
                        true,
                        new [] { "some string" }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_CollectionData))]
        public void GetCurrentValues_WithModelStateEntryAndViewData_ReturnsModelStateEntry(
            string[] rawValue,
            bool allowMultiple,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";
            viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Name),
                allowMultiple: allowMultiple);

            // Assert
            Assert.NotNull(result);
            Assert.Equal<string>(expected, result);
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_CollectionData))]
        public void GetCurrentValues_WithModelStateEntryModelExplorerAndViewData_ReturnsModelStateEntry(
            string[] rawValue,
            bool allowMultiple,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";
            viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), "ignored model value");

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer,
                expression: nameof(Model.Name),
                allowMultiple: allowMultiple);

            // Assert
            Assert.NotNull(result);
            Assert.Equal<string>(expected, result);
        }

        // rawValue -> expected current values
        public static TheoryData<string[], string[]> GetCurrentValues_StringData
        {
            get
            {
                return new TheoryData<string[], string[]>
                {
                    // 1. If given a ModelExplorer, GetCurrentValues does not use ViewData even if expression result is
                    // null.
                    // 2. Otherwise if ViewData entry exists, GetCurrentValue does not fall back to ViewData.Model even
                    // if entry is null.
                    // 3. Otherwise, GetCurrentValue does not fall back anywhere else even if ViewData.Model is null.
                    { null, null },
                    { new string[] { string.Empty }, new [] { string.Empty } },
                    { new string[] { "some string" }, new [] { "some string" } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_StringData))]
        public void GetCurrentValues_WithModelExplorerAndViewData_ReturnsExpressionResult(
            string[] rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";
            viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), rawValue);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer,
                expression: nameof(Model.Name),
                allowMultiple: false);

            // Assert
            Assert.Equal<string>(expected, result);
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_StringData))]
        public void GetCurrentValues_WithViewData_ReturnsViewDataEntry(
            string[] rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = rawValue;
            viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Name),
                allowMultiple: false);

            // Assert
            Assert.Equal<string>(expected, result);
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_StringData))]
        public void GetCurrentValues_WithModel_ReturnsModel(string[] rawValue, IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = rawValue?[0] };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ModelState.SetModelValue(nameof(Model.Name), rawValue, attemptedValue: null);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Name),
                allowMultiple: false);

            // Assert
            Assert.Equal<string>(expected, result);
        }

        // rawValue -> expected current values
        public static TheoryData<string[], string[]> GetCurrentValues_StringCollectionData
        {
            get
            {
                return new TheoryData<string[], string[]>
                {
                    { new string[] { null }, new [] { string.Empty } },
                    { new [] { string.Empty }, new [] { string.Empty } },
                    { new [] { "some string" }, new [] { "some string" } },
                    { new [] { "some string", "some other string" }, new [] { "some string", "some other string" } },
                    {
                        new [] { null, "some string", "some other string" },
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores duplicates
                    {
                        new [] { null, "some string", null, "some other string", null, "some string", null },
                        new [] { string.Empty, "some string", "some other string" }
                    },
                    // ignores case of duplicates
                    {
                        new [] { "some string", "SoMe StriNg", "Some String", "soME STRing", "SOME STRING" },
                        new [] { "some string" }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_StringCollectionData))]
        public void GetCurrentValues_CollectionWithModelExplorerAndViewData_ReturnsExpressionResult(
            string[] rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Collection = { "ignored property value" } };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Collection)] = new[] { "ignored ViewData value" };
            viewContext.ModelState.SetModelValue(nameof(Model.Collection), rawValue, attemptedValue: null);

            var modelExplorer =
                metadataProvider.GetModelExplorerForType(typeof(List<string>), new List<string>(rawValue));

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer,
                expression: nameof(Model.Collection),
                allowMultiple: true);

            // Assert
            Assert.Equal<string>(expected, result);
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_StringCollectionData))]
        public void GetCurrentValues_CollectionWithViewData_ReturnsViewDataEntry(
            string[] rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Collection = { "ignored property value" } };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Collection)] = rawValue;
            viewContext.ModelState.SetModelValue(nameof(Model.Collection), rawValue, attemptedValue: null);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Collection),
                allowMultiple: true);

            // Assert
            Assert.Equal<string>(expected, result);
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_StringCollectionData))]
        public void GetCurrentValues_CollectionWithModel_ReturnsModel(
            string[] rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model();
            model.Collection.AddRange(rawValue);

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ModelState.SetModelValue(
                nameof(Model.Collection),
                rawValue,
                attemptedValue: null);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: nameof(Model.Collection),
                allowMultiple: true);

            // Assert
            Assert.Equal<string>(expected, result);
        }

        // property name, rawValue -> expected current values
        public static TheoryData<string, object, string[]> GetCurrentValues_ValueToConvertData
        {
            get
            {
                return new TheoryData<string, object, string[]>
                {
                    { nameof(Model.FlagsEnum), FlagsEnum.All, new [] { "-1", "All" } },
                    { nameof(Model.FlagsEnum), FlagsEnum.FortyTwo, new [] { "42", "FortyTwo" } },
                    { nameof(Model.FlagsEnum), FlagsEnum.None, new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), FlagsEnum.Two, new [] { "2", "Two" } },
                    { nameof(Model.FlagsEnum), string.Empty, new [] { string.Empty } },
                    { nameof(Model.FlagsEnum), "All", new [] { "-1", "All" } },
                    { nameof(Model.FlagsEnum), "FortyTwo", new [] { "42", "FortyTwo" } },
                    { nameof(Model.FlagsEnum), "None", new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), "Two", new [] { "2", "Two" } },
                    { nameof(Model.FlagsEnum), "Two, Four", new [] { "Two, Four", "6" } },
                    { nameof(Model.FlagsEnum), "garbage", new [] { "garbage" } },
                    { nameof(Model.FlagsEnum), "0", new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), "   43", new [] { "   43", "43" } },
                    { nameof(Model.FlagsEnum), "-5   ", new [] { "-5   ", "-5" } },
                    { nameof(Model.FlagsEnum), 0, new [] { "0", "None" } },
                    { nameof(Model.FlagsEnum), 1, new [] { "1", "One" } },
                    { nameof(Model.FlagsEnum), 43, new [] { "43" } },
                    { nameof(Model.FlagsEnum), -5, new [] { "-5" } },
                    { nameof(Model.FlagsEnum), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.FlagsEnum), (uint)int.MaxValue + 1, new [] { "2147483648" } },
                    { nameof(Model.FlagsEnum), uint.MaxValue, new [] { "4294967295" } },  // converted to string & used

                    { nameof(Model.Id), string.Empty, new [] { string.Empty } },
                    { nameof(Model.Id), "garbage", new [] { "garbage" } },                  // no compatibility checks
                    { nameof(Model.Id), "0", new [] { "0" } },
                    { nameof(Model.Id), "  43", new [] { "  43" } },
                    { nameof(Model.Id), "-5  ", new [] { "-5  " } },
                    { nameof(Model.Id), 0, new [] { "0" } },
                    { nameof(Model.Id), 1, new [] { "1" } },
                    { nameof(Model.Id), 43, new [] { "43" } },
                    { nameof(Model.Id), -5, new [] { "-5" } },
                    { nameof(Model.Id), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.Id), (uint)int.MaxValue + 1, new [] { "2147483648" } },  // no limit checks
                    { nameof(Model.Id), uint.MaxValue, new [] { "4294967295" } },           // no limit checks

                    { nameof(Model.NullableEnum), RegularEnum.Zero, new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), RegularEnum.One, new [] { "1", "One" } },
                    { nameof(Model.NullableEnum), RegularEnum.Two, new [] { "2", "Two" } },
                    { nameof(Model.NullableEnum), RegularEnum.Three, new [] { "3", "Three" } },
                    { nameof(Model.NullableEnum), string.Empty, new [] { string.Empty } },
                    { nameof(Model.NullableEnum), "Zero", new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), "Two", new [] { "2", "Two" } },
                    { nameof(Model.NullableEnum), "One, Two", new [] { "One, Two", "3", "Three" } },
                    { nameof(Model.NullableEnum), "garbage", new [] { "garbage" } },
                    { nameof(Model.NullableEnum), "0", new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), "   43", new [] { "   43", "43" } },
                    { nameof(Model.NullableEnum), "-5   ", new [] { "-5   ", "-5" } },
                    { nameof(Model.NullableEnum), 0, new [] { "0", "Zero" } },
                    { nameof(Model.NullableEnum), 1, new [] { "1", "One" } },
                    { nameof(Model.NullableEnum), 43, new [] { "43" } },
                    { nameof(Model.NullableEnum), -5, new [] { "-5" } },
                    { nameof(Model.NullableEnum), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.NullableEnum), (uint)int.MaxValue + 1, new [] { "2147483648" } },
                    { nameof(Model.NullableEnum), uint.MaxValue, new [] { "4294967295" } },

                    { nameof(Model.RegularEnum), RegularEnum.Zero, new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), RegularEnum.One, new [] { "1", "One" } },
                    { nameof(Model.RegularEnum), RegularEnum.Two, new [] { "2", "Two" } },
                    { nameof(Model.RegularEnum), RegularEnum.Three, new [] { "3", "Three" } },
                    { nameof(Model.RegularEnum), string.Empty, new [] { string.Empty } },
                    { nameof(Model.RegularEnum), "Zero", new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), "Two", new [] { "2", "Two" } },
                    { nameof(Model.RegularEnum), "One, Two", new [] { "One, Two", "3", "Three" } },
                    { nameof(Model.RegularEnum), "garbage", new [] { "garbage" } },
                    { nameof(Model.RegularEnum), "0", new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), "   43", new [] { "   43", "43" } },
                    { nameof(Model.RegularEnum), "-5   ", new [] { "-5   ", "-5" } },
                    { nameof(Model.RegularEnum), 0, new [] { "0", "Zero" } },
                    { nameof(Model.RegularEnum), 1, new [] { "1", "One" } },
                    { nameof(Model.RegularEnum), 43, new [] { "43" } },
                    { nameof(Model.RegularEnum), -5, new [] { "-5" } },
                    { nameof(Model.RegularEnum), int.MaxValue, new [] { "2147483647" } },
                    { nameof(Model.RegularEnum), (uint)int.MaxValue + 1, new [] { "2147483648" } },
                    { nameof(Model.RegularEnum), uint.MaxValue, new [] { "4294967295" } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_ValueToConvertData))]
        public void GetCurrentValues_ValueConvertedAsExpected(
            string propertyName,
            object rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var viewContext = GetViewContext<Model>(model: null, metadataProvider: metadataProvider);
            viewContext.ModelState.SetModelValue(
                propertyName,
                new string[] { rawValue.ToString() },
                attemptedValue: null);

            // Act
            var result = htmlGenerator.GetCurrentValues(
                viewContext,
                modelExplorer: null,
                expression: propertyName,
                allowMultiple: false);

            // Assert
            Assert.Equal<string>(expected, result);
        }

        // GetCurrentValues uses only the IModelMetadataProvider passed to the DefaultHtmlGenerator constructor.
        private static IHtmlGenerator GetGenerator(IModelMetadataProvider metadataProvider)
        {
            var mvcViewOptionsAccessor = new Mock<IOptions<MvcViewOptions>>();
            mvcViewOptionsAccessor.SetupGet(accessor => accessor.Value).Returns(new MvcViewOptions());
            var htmlEncoder = Mock.Of<HtmlEncoder>();
            var antiforgery = Mock.Of<IAntiforgery>();

            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor
                .SetupGet(o => o.Value)
                .Returns(new MvcOptions());

            return new DefaultHtmlGenerator(
                antiforgery,
                mvcViewOptionsAccessor.Object,
                metadataProvider,
                Mock.Of<IUrlHelper>(),
                htmlEncoder);
        }

        // GetCurrentValues uses only the ModelStateDictionary and ViewDataDictionary from the passed ViewContext.
        private static ViewContext GetViewContext<TModel>(TModel model, IModelMetadataProvider metadataProvider)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary<TModel>(metadataProvider, actionContext.ModelState)
            {
                Model = model,
            };

            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());
        }

        public enum RegularEnum
        {
            Zero,
            One,
            Two,
            Three,
        }

        public enum FlagsEnum
        {
            None = 0,
            One = 1,
            Two = 2,
            Four = 4,
            FortyTwo = 42,
            All = -1,
        }

        private class Model
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public RegularEnum RegularEnum { get; set; }

            public FlagsEnum FlagsEnum { get; set; }

            public RegularEnum? NullableEnum { get; set; }

            public List<string> Collection { get; } = new List<string>();
        }
    }
}
#endif