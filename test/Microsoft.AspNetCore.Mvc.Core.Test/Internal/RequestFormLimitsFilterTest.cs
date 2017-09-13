// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class RequestFormLimitsFilterTest
    {
        [Fact]
        public void SetsRequestFormFeature_WhenFeatureIsNotPresent()
        {
            // Arrange
            var requestFormLimitsFilter = new RequestFormLimitsFilter(NullLoggerFactory.Instance);
            requestFormLimitsFilter.FormOptions = new FormOptions();
            var authorizationFilterContext = CreateauthorizationFilterContext(
                new IFilterMetadata[] { requestFormLimitsFilter });
            // Set to null explicitly as we want to make sure the filter adds one
            authorizationFilterContext.HttpContext.Features.Set<IFormFeature>(null);

            // Act
            requestFormLimitsFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            var formFeature = authorizationFilterContext.HttpContext.Features.Get<IFormFeature>();
            Assert.IsType<FormFeature>(formFeature);
        }

        [Fact]
        public void SetsRequestFormFeature_WhenFeatureIsPresent_ButFormIsNull()
        {
            // Arrange
            var requestFormLimitsFilter = new RequestFormLimitsFilter(NullLoggerFactory.Instance);
            requestFormLimitsFilter.FormOptions = new FormOptions();
            var authorizationFilterContext = CreateauthorizationFilterContext(
                new IFilterMetadata[] { requestFormLimitsFilter });
            var oldFormFeature = new FormFeature(authorizationFilterContext.HttpContext.Request);
            // Set to null explicitly as we want to make sure the filter adds one
            authorizationFilterContext.HttpContext.Features.Set<IFormFeature>(oldFormFeature);

            // Act
            requestFormLimitsFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            var actualFormFeature = authorizationFilterContext.HttpContext.Features.Get<IFormFeature>();
            Assert.NotSame(oldFormFeature, actualFormFeature);
        }

        [Fact]
        public void LogsCannotApplyRequestFormLimits()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var requestFormLimitsFilter = new RequestFormLimitsFilter(loggerFactory);
            requestFormLimitsFilter.FormOptions = new FormOptions();
            var authorizationFilterContext = CreateauthorizationFilterContext(
                new IFilterMetadata[] { requestFormLimitsFilter });
            authorizationFilterContext.HttpContext.Request.Form = new FormCollection(null);

            // Act
            requestFormLimitsFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal(LogLevel.Warning, write.LogLevel);
            Assert.Equal(
                "Unable to apply configured form options since the request form has already been read.",
                write.State.ToString());
        }

        [Fact]
        public void LogsAppliedRequestFormLimits_WhenFormFeatureIsNull()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var requestFormLimitsFilter = new RequestFormLimitsFilter(loggerFactory);
            requestFormLimitsFilter.FormOptions = new FormOptions();
            var authorizationFilterContext = CreateauthorizationFilterContext(
                new IFilterMetadata[] { requestFormLimitsFilter });
            // Set to null explicitly as we want to make sure the filter adds one
            authorizationFilterContext.HttpContext.Features.Set<IFormFeature>(null);

            // Act
            requestFormLimitsFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal(LogLevel.Debug, write.LogLevel);
            Assert.Equal(
                "Applied the configured form options on the current request.",
                write.State.ToString());
        }

        [Fact]
        public void LogsAppliedRequestFormLimits_WhenFormFeatureIsPresent_ButFormIsNull()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);

            var requestFormLimitsFilter = new RequestFormLimitsFilter(loggerFactory);
            requestFormLimitsFilter.FormOptions = new FormOptions();
            var authorizationFilterContext = CreateauthorizationFilterContext(
                new IFilterMetadata[] { requestFormLimitsFilter });
            // Set to null explicitly as we want to make sure the filter adds one
            authorizationFilterContext.HttpContext.Features.Set<IFormFeature>(
                new FormFeature(authorizationFilterContext.HttpContext.Request));

            // Act
            requestFormLimitsFilter.OnAuthorization(authorizationFilterContext);

            // Assert
            var write = Assert.Single(sink.Writes);
            Assert.Equal(LogLevel.Debug, write.LogLevel);
            Assert.Equal(
                "Applied the configured form options on the current request.",
                write.State.ToString());
        }

        private static AuthorizationFilterContext CreateauthorizationFilterContext(IFilterMetadata[] filters)
        {
            return new AuthorizationFilterContext(CreateActionContext(), filters);
        }

        private static ActionContext CreateActionContext()
        {
            return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        }
    }
}
