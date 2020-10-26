// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Cors;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    // These tests are intentionally in Mvc.Test so we can also test the CORS action constraint.
    public class ActionConstraintMatcherPolicyTest
    {
        [Fact]
        public async Task Apply_CanBeAmbiguous()
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
            await selector.ApplyAsync(new DefaultHttpContext(), candidateSet);

            // Assert
            Assert.True(candidateSet.IsValidCandidate(0));
            Assert.True(candidateSet.IsValidCandidate(1));
        }

        [Fact]
        public async Task Apply_PrefersActionWithConstraints()
        {
            // Arrange
            var actionWithConstraints = new ActionDescriptor()
            {
                ActionConstraints = new List<IActionConstraintMetadata>()
                {
                    new BooleanConstraint() { Pass = true, },
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
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet.IsValidCandidate(0));
            Assert.False(candidateSet.IsValidCandidate(1));
        }

        [Fact]
        public async Task Apply_ConstraintsRejectAll()
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
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet.IsValidCandidate(0));
            Assert.False(candidateSet.IsValidCandidate(1));
        }

        [Fact]
        public async Task Apply_ConstraintsRejectAll_DifferentStages()
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
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet.IsValidCandidate(0));
            Assert.False(candidateSet.IsValidCandidate(1));
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public async Task Apply_ConstraintsInOrder()
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
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet.IsValidCandidate(0));
            Assert.False(candidateSet.IsValidCandidate(1));
        }

        [Fact]
        public async Task Apply_SkipsOverInvalidEndpoints()
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
            candidateSet.SetValidity(0, false);
            candidateSet.SetValidity(1, false);

            var selector = CreateSelector(actions);
            var httpContext = CreateHttpContext("POST");

            // Act
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet.IsValidCandidate(0));
            Assert.False(candidateSet.IsValidCandidate(1));
            Assert.True(candidateSet.IsValidCandidate(2));
        }

        [Fact]
        public async Task Apply_IncludesNonMvcEndpoints()
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
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.False(candidateSet.IsValidCandidate(0));
            Assert.True(candidateSet.IsValidCandidate(1));
            Assert.False(candidateSet.IsValidCandidate(2));
        }

        // Due to ordering of stages, the first action will be better.
        [Fact]
        public async Task Apply_ConstraintsInOrder_MultipleStages()
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
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet.IsValidCandidate(0));
            Assert.False(candidateSet.IsValidCandidate(1));
        }

        [Fact]
        public async Task Apply_Fallback_ToActionWithoutConstraints()
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
            await selector.ApplyAsync(httpContext, candidateSet);

            // Assert
            Assert.True(candidateSet.IsValidCandidate(0));
            Assert.False(candidateSet.IsValidCandidate(1));
            Assert.False(candidateSet.IsValidCandidate(2));
        }

        [Fact]
        public void AppliesToEndpoints_IgnoresIgnorableConstraints()
        {
            // Arrange
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {

                },
                new ActionDescriptor()
                {
                    ActionConstraints = new List<IActionConstraintMetadata>()
                    {
                        new HttpMethodActionConstraint(new[]{ "GET", }),
                    },
                },
                new ActionDescriptor()
                {
                    ActionConstraints = new List<IActionConstraintMetadata>()
                    {
                        new ConsumesAttribute("text/json"),
                    },
                },
            };
            var endpoints = actions.Select(CreateEndpoint).ToArray();

            var selector = CreateSelector(actions);

            // Act
            var result = selector.AppliesToEndpoints(endpoints);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRunActionConstraints_RunsForArbitraryActionConstraint()
        {
            // Arrange
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {

                },
                new ActionDescriptor()
                {
                    ActionConstraints = new List<IActionConstraintMetadata>()
                    {
                        new BooleanConstraint(),
                    },
                },
            };
            var endpoints = actions.Select(CreateEndpoint).ToArray();

            var selector = CreateSelector(actions);

            // Act
            var result = selector.AppliesToEndpoints(endpoints);

            // Assert
            Assert.True(result);
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

            var actionDescriptorCollectionProvider = new DefaultActionDescriptorCollectionProvider(
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

        private static Endpoint CreateEndpoint(ActionDescriptor action)
        {
            var metadata = new List<object>() { action, };
            return new Endpoint(
                (context) => Task.CompletedTask,
                new EndpointMetadataCollection(metadata),
                $"test: {action?.DisplayName}");
        }

        private static CandidateSet CreateCandidateSet(ActionDescriptor[] actions)
        {
            var values = new RouteValueDictionary[actions.Length];
            for (var i = 0; i < actions.Length; i++)
            {
                values[i] = new RouteValueDictionary();
            }

            var candidateSet = new CandidateSet(
                actions.Select(CreateEndpoint).ToArray(),
                values,
                new int[actions.Length]);
            return candidateSet;
        }

        private static ActionConstraintCache GetActionConstraintCache(IActionConstraintProvider[] actionConstraintProviders = null)
        {
            var descriptorProvider = new DefaultActionDescriptorCollectionProvider(
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
