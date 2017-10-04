// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class ApiBehaviorApiDescriptionProviderTest
    {
        [Fact]
        public void AppliesTo_ActionWithoutApiBehavior_ReturnsFalse()
        {
            // Arrange
            var action = new ActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>(),
            };
            var description = new ApiDescription()
            {
                ActionDescriptor = action,
            };

            var provider = new ApiBehaviorApiDescriptionProvider(new EmptyModelMetadataProvider());

            // Act
            var result = provider.AppliesTo(description);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AppliesTo_ActionWithApiBehavior_ReturnsTrue()
        {
            // Arrange
            var action = new ActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>()
                {
                    new FilterDescriptor(Mock.Of<IApiBehaviorMetadata>(), FilterScope.Global),
                }
            };
            var description = new ApiDescription()
            {
                ActionDescriptor = action,
            };

            var provider = new ApiBehaviorApiDescriptionProvider(new EmptyModelMetadataProvider());

            // Act
            var result = provider.AppliesTo(description);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("id")]
        [InlineData("personId")]
        [InlineData("üId")]
        public void IsIdParameter_ParameterNameMatchesConvention_ReturnsTrue(string name)
        {
            var parameter = new ParameterDescriptor()
            {
                Name = name,
            };

            var provider = new ApiBehaviorApiDescriptionProvider(new EmptyModelMetadataProvider());

            // Act
            var result = provider.IsIdParameter(parameter);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("i")]
        [InlineData("Id")]
        [InlineData("iD")]
        [InlineData("persoNId")]
        [InlineData("personid")]
        [InlineData("ü Id")]
        [InlineData("ÜId")]
        public void IsIdParameter_ParameterNameDoesNotMatchConvention_ReturnsFalse(string name)
        {
            var parameter = new ParameterDescriptor()
            {
                Name = name,
            };

            var provider = new ApiBehaviorApiDescriptionProvider(new EmptyModelMetadataProvider());

            // Act
            var result = provider.IsIdParameter(parameter);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CreateProblemResponseTypes_NoParameters_IncludesDefaultResponse()
        {
            // Arrange
            var action = new ActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>()
                {
                    new FilterDescriptor(Mock.Of<IApiBehaviorMetadata>(), FilterScope.Global),
                },
                BoundProperties = new List<ParameterDescriptor>(),
                Parameters = new List<ParameterDescriptor>(),
            };
            var description = new ApiDescription()
            {
                ActionDescriptor = action,
            };

            var provider = new ApiBehaviorApiDescriptionProvider(new EmptyModelMetadataProvider());

            // Act
            var results = provider.CreateProblemResponseTypes(description);

            // Assert
            Assert.Collection(
                results.OrderBy(r => r.StatusCode),
                r =>
                {
                    Assert.Equal(typeof(ProblemDetails), r.Type);
                    Assert.Equal(0, r.StatusCode);
                    Assert.True(r.IsDefaultResponse);
                });
        }

        [Fact]
        public void CreateProblemResponseTypes_WithBoundProperty_Includes400Response()
        {
            // Arrange
            var action = new ActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>()
                {
                    new FilterDescriptor(Mock.Of<IApiBehaviorMetadata>(), FilterScope.Global),
                },
                BoundProperties = new List<ParameterDescriptor>()
                {
                    new ParameterDescriptor()
                },
                Parameters = new List<ParameterDescriptor>(),
            };
            var description = new ApiDescription()
            {
                ActionDescriptor = action,
            };

            var provider = new ApiBehaviorApiDescriptionProvider(new EmptyModelMetadataProvider());

            // Act
            var results = provider.CreateProblemResponseTypes(description);

            // Assert
            Assert.Collection(
                results.OrderBy(r => r.StatusCode),
                r =>
                {
                    Assert.Equal(typeof(ProblemDetails), r.Type);
                    Assert.Equal(0, r.StatusCode);
                    Assert.True(r.IsDefaultResponse);
                },
                r =>
                {
                    Assert.Equal(typeof(ProblemDetails), r.Type);
                    Assert.Equal(400, r.StatusCode);
                    Assert.False(r.IsDefaultResponse);
                });
        }

        [Fact]
        public void CreateProblemResponseTypes_WithIdParameter_Includes404Response()
        {
            // Arrange
            var action = new ActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>()
                {
                    new FilterDescriptor(Mock.Of<IApiBehaviorMetadata>(), FilterScope.Global),
                },
                BoundProperties = new List<ParameterDescriptor>()
                {
                },
                Parameters = new List<ParameterDescriptor>()
                {
                    new ParameterDescriptor()
                    {
                        Name = "customerId",
                    }
                },
            };
            var description = new ApiDescription()
            {
                ActionDescriptor = action,
            };

            var provider = new ApiBehaviorApiDescriptionProvider(new EmptyModelMetadataProvider());

            // Act
            var results = provider.CreateProblemResponseTypes(description);

            // Assert
            Assert.Collection(
                results.OrderBy(r => r.StatusCode),
                r =>
                {
                    Assert.Equal(typeof(ProblemDetails), r.Type);
                    Assert.Equal(0, r.StatusCode);
                    Assert.True(r.IsDefaultResponse);
                },
                r =>
                {
                    Assert.Equal(typeof(ProblemDetails), r.Type);
                    Assert.Equal(400, r.StatusCode);
                    Assert.False(r.IsDefaultResponse);
                },
                r =>
                {
                    Assert.Equal(typeof(ProblemDetails), r.Type);
                    Assert.Equal(404, r.StatusCode);
                    Assert.False(r.IsDefaultResponse);
                });
        }
    }
}
