// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorPageCreateModelExpressionTest
    {
        public static TheoryData<Expression<Func<RazorPageCreateModelExpressionModel, int>>, string> IntExpressions
        {
            get
            {
                var somethingElse = 23;
                return new TheoryData<Expression<Func<RazorPageCreateModelExpressionModel, int>>, string>
                {
                    { model => somethingElse, "somethingElse" },
                    { model => model.Id, "Id" },
                    { model => model.SubModel.Id, "SubModel.Id" },
                    { model => model.SubModel.SubSubModel.Id, "SubModel.SubSubModel.Id" },
                };
            }
        }

        public static TheoryData<Expression<Func<RazorPageCreateModelExpressionModel, string>>, string> StringExpressions
        {
            get
            {
                var somethingElse = "This is something else";
                return new TheoryData<Expression<Func<RazorPageCreateModelExpressionModel, string>>, string>
                {
                    { model => somethingElse, "somethingElse" },
                    { model => model.Name, "Name" },
                    { model => model.SubModel.Name, "SubModel.Name" },
                    { model => model.SubModel.SubSubModel.Name, "SubModel.SubSubModel.Name" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IntExpressions))]
        public void CreateModelExpression_ReturnsExpectedMetadata_IntExpressions(
            Expression<Func<RazorPageCreateModelExpressionModel, int>> expression,
            string expectedName)
        {
            // Arrange
            var viewContext = CreateViewContext(model: null);
            var page = CreatePage(viewContext);

            // Act
            var result = page.CreateModelExpression(expression);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Metadata);
            Assert.Equal(typeof(int), result.Metadata.ModelType);
            Assert.Equal(expectedName, result.Name);
        }

        [Theory]
        [MemberData(nameof(StringExpressions))]
        public void CreateModelExpression_ReturnsExpectedMetadata_StringExpressions(
            Expression<Func<RazorPageCreateModelExpressionModel, string>> expression,
            string expectedName)
        {
            // Arrange
            var viewContext = CreateViewContext(model: null);
            var page = CreatePage(viewContext);

            // Act
            var result = page.CreateModelExpression(expression);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Metadata);
            Assert.Equal(typeof(string), result.Metadata.ModelType);
            Assert.Equal(expectedName, result.Name);
        }

        private static TestRazorPage CreatePage(ViewContext viewContext)
        {
            return new TestRazorPage
            {
                ViewContext = viewContext,
                ViewData = (ViewDataDictionary<RazorPageCreateModelExpressionModel>)viewContext.ViewData,
            };
        }

        private static ViewContext CreateViewContext(RazorPageCreateModelExpressionModel model)
        {
            return CreateViewContext(model, new DataAnnotationsModelMetadataProvider());
        }

        private static ViewContext CreateViewContext(
            RazorPageCreateModelExpressionModel model,
            IModelMetadataProvider provider)
        {
            var viewData = new ViewDataDictionary<RazorPageCreateModelExpressionModel>(provider)
            {
                Model = model,
            };

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(real => real.GetService(typeof(IModelMetadataProvider)))
                .Returns(provider);

            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(real => real.RequestServices)
                .Returns(serviceProvider.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

            return new ViewContext(
                actionContext,
                view: Mock.Of<IView>(),
                viewData: viewData,
                writer: new StringWriter());
        }

        private class TestRazorPage : RazorPage<RazorPageCreateModelExpressionModel>
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        public class RazorPageCreateModelExpressionModel
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public RazorPageCreateModelExpressionSubModel SubModel { get; set; }
        }

        public class RazorPageCreateModelExpressionSubModel
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public RazorPageCreateModelExpressionSubSubModel SubSubModel { get; set; }
        }

        public class RazorPageCreateModelExpressionSubSubModel
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}