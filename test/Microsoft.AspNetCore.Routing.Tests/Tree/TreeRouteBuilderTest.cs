// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Tree
{
    public class TreeRouteBuilderTest
    {
        [Fact]
        public void TreeRouter_BuildThrows_RoutesWithTheSameNameAndDifferentTemplates()
        {
            // Arrange
            var builder = CreateBuilder();

            var message = "Two or more routes named 'Get_Products' have different templates.";

            builder.MapOutbound(
                Mock.Of<IRouter>(),
                TemplateParser.Parse("api/Products"),
                new RouteValueDictionary(),
                "Get_Products",
                order: 0);

            builder.MapOutbound(
                Mock.Of<IRouter>(),
                TemplateParser.Parse("Products/Index"),
                new RouteValueDictionary(),
                "Get_Products",
                order: 0);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() =>
            {
                builder.Build();
            }, "linkGenerationEntries", message);
        }

        [Fact]
        public void TreeRouter_BuildDoesNotThrow_RoutesWithTheSameNameAndSameTemplates()
        {
            // Arrange
            var builder = CreateBuilder();

            builder.MapOutbound(
                Mock.Of<IRouter>(),
                TemplateParser.Parse("api/Products"),
                new RouteValueDictionary(),
                "Get_Products",
                order: 0);

            builder.MapOutbound(
                Mock.Of<IRouter>(),
                TemplateParser.Parse("api/products"),
                new RouteValueDictionary(),
                "Get_Products",
                order: 0);

            // Act & Assert (does not throw)
            builder.Build();
        }

        private static TreeRouteBuilder CreateBuilder()
        {
            var objectPoolProvider = new DefaultObjectPoolProvider();
            var objectPolicy = new UriBuilderContextPooledObjectPolicy(UrlEncoder.Default);
            var objectPool = objectPoolProvider.Create<UriBuildingContext>(objectPolicy);

            var constraintResolver = Mock.Of<IInlineConstraintResolver>();
            var builder = new TreeRouteBuilder(
                NullLoggerFactory.Instance,
                UrlEncoder.Default,
                objectPool,
                constraintResolver);
            return builder;
        }
    }
}
