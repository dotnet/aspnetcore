// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
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
        public static TheoryData<object, bool, IReadOnlyCollection<string>> GetCurrentValues_StringAndCollectionData
        {
            get
            {
                return new TheoryData<object, bool, IReadOnlyCollection<string>>
                {
                    // ModelStateDictionary converts single values to arrays and visa-versa.
                    { string.Empty, false, new [] { string.Empty } },
                    { string.Empty, true, new [] { string.Empty } },
                    { "some string", false,  new [] { "some string" } },
                    { "some string", true, new [] { "some string" } },
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
        [MemberData(nameof(GetCurrentValues_StringAndCollectionData))]
        public void GetCurrentValues_WithModelStateEntryAndViewData_ReturnsModelStateEntry(
            object rawValue,
            bool allowMultiple,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";


            var valueProviderResult = new ValueProviderResult(
                rawValue,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Name), valueProviderResult);

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
        [MemberData(nameof(GetCurrentValues_StringAndCollectionData))]
        public void GetCurrentValues_WithModelStateEntryModelExplorerAndViewData_ReturnsModelStateEntry(
            object rawValue,
            bool allowMultiple,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";

            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), "ignored model value");

            var valueProviderResult = new ValueProviderResult(
                rawValue,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Name), valueProviderResult);

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
        public static TheoryData<string, string[]> GetCurrentValues_StringData
        {
            get
            {
                return new TheoryData<string, string[]>
                {
                    // 1. If given a ModelExplorer, GetCurrentValues does not use ViewData even if expression result is
                    // null.
                    // 2. Otherwise if ViewData entry exists, GetCurrentValue does not fall back to ViewData.Model even
                    // if entry is null.
                    // 3. Otherwise, GetCurrentValue does not fall back anywhere else even if ViewData.Model is null.
                    { null, null },
                    { string.Empty, new [] { string.Empty } },
                    { "some string", new [] { "some string" } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetCurrentValues_StringData))]
        public void GetCurrentValues_WithModelExplorerAndViewData_ReturnsExpressionResult(
            string rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = "ignored ViewData value";

            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), rawValue);

            var valueProviderResult = new ValueProviderResult(
                rawValue: null,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Name), valueProviderResult);

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
            object rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = "ignored property value" };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Name)] = rawValue;

            var valueProviderResult = new ValueProviderResult(
                rawValue: null,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Name), valueProviderResult);

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
        public void GetCurrentValues_WithModel_ReturnsModel(string rawValue, IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Name = rawValue };

            var viewContext = GetViewContext<Model>(model, metadataProvider);

            var valueProviderResult = new ValueProviderResult(
                rawValue: null,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Name), valueProviderResult);

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

            var modelExplorer =
                metadataProvider.GetModelExplorerForType(typeof(List<string>), new List<string>(rawValue));

            var valueProviderResult = new ValueProviderResult(
                rawValue: null,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Collection), valueProviderResult);

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
            object[] rawValue,
            IReadOnlyCollection<string> expected)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = GetGenerator(metadataProvider);
            var model = new Model { Collection = { "ignored property value" } };

            var viewContext = GetViewContext<Model>(model, metadataProvider);
            viewContext.ViewData[nameof(Model.Collection)] = rawValue;

            var valueProviderResult = new ValueProviderResult(
                rawValue: null,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Collection), valueProviderResult);

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

            var valueProviderResult = new ValueProviderResult(
                rawValue: null,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(nameof(Model.Collection), valueProviderResult);

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

            var valueProviderResult = new ValueProviderResult(
                rawValue,
                attemptedValue: null,
                culture: CultureInfo.InvariantCulture);
            viewContext.ModelState.SetModelValue(propertyName, valueProviderResult);

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
            var mvcOptionsAccessor = new Mock<IOptions<MvcOptions>>();
            mvcOptionsAccessor.SetupGet(accessor => accessor.Options).Returns(new MvcOptions());
            var htmlEncoder = Mock.Of<IHtmlEncoder>();
            var dataOptionsAccessor = new Mock<IOptions<DataProtectionOptions>>();
            dataOptionsAccessor.SetupGet(accessor => accessor.Options).Returns(new DataProtectionOptions());
            var antiForgery = new AntiForgery(
                Mock.Of<IClaimUidExtractor>(),
                Mock.Of<IDataProtectionProvider>(),
                Mock.Of<IAntiForgeryAdditionalDataProvider>(),
                mvcOptionsAccessor.Object,
                htmlEncoder,
                dataOptionsAccessor.Object);

            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor
                .SetupGet(o => o.Options)
                .Returns(new MvcOptions());

            return new DefaultHtmlGenerator(
                antiForgery,
                optionsAccessor.Object,
                metadataProvider,
                Mock.Of<IUrlHelper>(),
                htmlEncoder);
        }

        // GetCurrentValues uses only the ModelStateDictionary and ViewDataDictionary from the passed ViewContext.
        private static ViewContext GetViewContext<TModel>(TModel model, IModelMetadataProvider metadataProvider)
        {
            var actionContext = new ActionContext();
            var viewData = new ViewDataDictionary<TModel>(metadataProvider, actionContext.ModelState)
            {
                Model = model,
            };

            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null);
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