// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class ActionConstraintMatcherPolicyTest
    {
        [Fact]
        public void Apply_CanBeAmbiguous()
        {
            // Arrange
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor() { DisplayName = "A1" },
                new ActionDescriptor() { DisplayName = "A2" },
            };

            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);

            // Act
            selector.Apply(new DefaultHttpContext(), candidateSet);

            // Assert
            Assert.True(candidateSet[0].IsValidCandidate);
            Assert.True(candidateSet[1].IsValidCandidate);
        }

        [Fact]
        public void Apply_PrefersActionWithConstraints()
        {
            // Arrange
            var actionWithConstraints = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new HttpMethodActionConstraint(new string[] { "POST" }),
                },
                Parameters = new List<ParameterDescriptor>(),
            };

            var actionWithoutConstraints = new ActionDescriptor()
            {
                Parameters = new List<ParameterDescriptor>(),
            };

            var actions = new ActionDescriptor[] { actionWithConstraints, actionWithoutConstraints };
            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);

            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet[0].IsValidCandidate);
            Assert.False(candidateSet[1].IsValidCandidate);
        }

        [Fact]
        public void Apply_ConstraintsRejectAll()
        {
            // Arrange
            var action1 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, },
                },
            };

            var action2 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, },
                },
            };

            var actions = new ActionDescriptor[] { action1, action2 };
            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);

            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet[0].IsValidCandidate);
            Assert.False(candidateSet[1].IsValidCandidate);
        }

        [Fact]
        public void Apply_ConstraintsRejectAll_DifferentStages()
        {
            // Arrange
            var action1 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, Order = 0 },
                    new BooleanConstraint() { Pass = true, Order = 1 },
                },
            };

            var action2 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0 },
                    new BooleanConstraint() { Pass = false, Order = 1 },
                },
            };

            var actions = new ActionDescriptor[] { action1, action2 };
            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);
            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet[0].IsValidCandidate);
            Assert.False(candidateSet[1].IsValidCandidate);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public void Apply_ConstraintsInOrder()
        {
            // Arrange
            var best = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                },
            };

            var worst = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 1, },
                },
            };

            var actions = new ActionDescriptor[] { best, worst };
            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);
            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet[0].IsValidCandidate);
            Assert.False(candidateSet[1].IsValidCandidate);
        }

        [Fact]
        public void Apply_SkipsOverInvalidEndpoints()
        {
            // Arrange
            var best = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                },
            };

            var another = new ActionDescriptor();

            var worst = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 1, },
                },
            };

            var actions = new ActionDescriptor[] { best, another, worst };
            var candidateSet = CreateCandidateSet(actions);
            candidateSet[0].IsValidCandidate = false;
            candidateSet[1].IsValidCandidate = false;

            var selector = CreateSelector(actions);
            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet[0].IsValidCandidate);
            Assert.False(candidateSet[1].IsValidCandidate);
            Assert.True(candidateSet[2].IsValidCandidate);
        }

        [Fact]
        public void Apply_IncludesNonMvcEndpoints()
        {
            // Arrange
            var action1 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, Order = 0, },
                },
            };

            var action2 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = false, Order = 1, },
                },
            };

            var actions = new ActionDescriptor[] { action1, null, action2 };
            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);
            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet[0].IsValidCandidate);
            Assert.True(candidateSet[1].IsValidCandidate);
            Assert.False(candidateSet[2].IsValidCandidate);
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public void Apply_ConstraintsInOrder_MultipleStages()
        {
            // Arrange
            var best = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = true, Order = 2, },
                },
            };

            var worst = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = true, Order = 3, },
                },
            };

            var actions = new ActionDescriptor[] { best, worst };
            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);

            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet[0].IsValidCandidate);
            Assert.False(candidateSet[1].IsValidCandidate);
        }

        [Fact]
        public void Apply_Fallback_ToActionWithoutConstraints()
        {
            // Arrange
            var nomatch1 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = false, Order = 2, },
                },
            };

            var nomatch2 = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, Order = 0, },
                    new BooleanConstraint() { Pass = true, Order = 1, },
                    new BooleanConstraint() { Pass = false, Order = 3, },
                },
            };

            var best = new ActionDescriptor();

            var actions = new ActionDescriptor[] { best, nomatch1, nomatch2 };
            var candidateSet = CreateCandidateSet(actions);

            var selector = CreateSelector(actions);

            var httpContext = CreateHttpContext("POST");

            // Act
            selector.Apply(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet[0].IsValidCandidate);
            Assert.False(candidateSet[1].IsValidCandidate);
            Assert.False(candidateSet[2].IsValidCandidate);
        }

        private ActionConstraintMatcherPolicy CreateSelector(ActionDescriptor[] actions)
        {
            // We need to actually provide some actions with some action constraints metadata
            // or else the policy will No-op.
            var actionDescriptorProvider = new Mock<IActionDescriptorProvider>();
            actionDescriptorProvider
                .Setup(a => a.OnProvidersExecuted(It.IsAny<ActionDescriptorProviderContext>()))
                .Callback<ActionDescriptorProviderContext>(c =>
                {
                    for (var i = 0; i < actions.Length; i++)
                    {
                        c.Results.Add(actions[i]);
                    }
                });

            var actionDescriptorCollectionProvider = new ActionDescriptorCollectionProvider(
                new IActionDescriptorProvider[] { actionDescriptorProvider.Object, },
                Enumerable.Empty<IActionDescriptorChangeProvider>());

            var cache = new ActionConstraintCache(actionDescriptorCollectionProvider, new[]
            {
                new DefaultActionConstraintProvider(),
            });

            return new ActionConstraintMatcherPolicy(cache);
        }

        private static HttpContext CreateHttpContext(string httpMethod)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = httpMethod;
            return httpContext;
        }

        private static MatcherEndpoint CreateEndpoint(ActionDescriptor action)
        {
            var metadata = new List<object>() { action, };
            return new MatcherEndpoint(
                (r) => null,
                RoutePatternFactory.Parse("/"),
                new RouteValueDictionary(),
                0,
                new EndpointMetadataCollection(metadata),
                $"test: {action?.DisplayName}");
        }

        private static CandidateSet CreateCandidateSet(ActionDescriptor[] actions)
        {
            var candidateSet = new CandidateSet(
                actions.Select(CreateEndpoint).ToArray(),
                new int[actions.Length]);

            for (var i = 0; i < actions.Length; i++)
            {
                if (candidateSet[i].IsValidCandidate)
                {
                    candidateSet[i].Values = new RouteValueDictionary();
                }
            }

            return candidateSet;
        }

        private static ActionConstraintCache GetActionConstraintCache(IActionConstraintProvider[] actionConstraintProviders = null)
        {
            var descriptorProvider = new ActionDescriptorCollectionProvider(
                Enumerable.Empty<IActionDescriptorProvider>(),
                Enumerable.Empty<IActionDescriptorChangeProvider>());
            return new ActionConstraintCache(descriptorProvider, actionConstraintProviders.AsEnumerable() ?? new List<IActionConstraintProvider>());
        }

        private class BooleanConstraint : IActionConstraint
        {
            public bool Pass { get; set; }

            public int Order { get; set; }

            public bool Accept(ActionConstraintContext context)
            {
                return Pass;
            }
        }
    }
}
