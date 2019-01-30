// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class DfaMatcherBuilderTest
    {
        [Fact]
        public void BuildDfaTree_SingleEndpoint_Empty()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("/");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches));
            Assert.Null(root.Parameters);
            Assert.Null(root.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_Literals()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Null(a.Matches);
            Assert.Null(a.Parameters);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Null(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Same(endpoint, Assert.Single(c.Matches));
            Assert.Null(c.Parameters);
            Assert.Null(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_Parameters()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("{a}/{b}/{c}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Literals);

            var a = root.Parameters;
            Assert.Null(a.Matches);
            Assert.Null(a.Literals);

            var b = a.Parameters;
            Assert.Null(b.Matches);
            Assert.Null(b.Literals);

            var c = b.Parameters;
            Assert.Same(endpoint, Assert.Single(c.Matches));
            Assert.Null(c.Parameters);
            Assert.Null(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_CatchAll()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("{a}/{*b}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Literals);

            var a = root.Parameters;

            // The catch all can match a path like '/a'
            Assert.Same(endpoint, Assert.Single(a.Matches));
            Assert.Null(a.Literals);
            Assert.Null(a.Parameters);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = a.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches));
            Assert.Null(catchAll.Literals);
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_CatchAllAtRoot()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("{*a}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches));
            Assert.Null(root.Literals);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = root.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches));
            Assert.Null(catchAll.Literals);
            Assert.Same(catchAll, catchAll.Parameters);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndLiteral()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b1/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/b2/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Null(a.Matches);

            Assert.Equal(2, a.Literals.Count);

            var b1 = a.Literals["b1"];
            Assert.Null(b1.Matches);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Same(endpoint1, Assert.Single(c1.Matches));
            Assert.Null(c1.Parameters);
            Assert.Null(c1.Literals);

            var b2 = a.Literals["b2"];
            Assert.Null(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches));
            Assert.Null(c2.Parameters);
            Assert.Null(c2.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralDifferentCase()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b1/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("A/b2/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Null(a.Matches);

            Assert.Equal(2, a.Literals.Count);

            var b1 = a.Literals["b1"];
            Assert.Null(b1.Matches);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Same(endpoint1, Assert.Single(c1.Matches));
            Assert.Null(c1.Parameters);
            Assert.Null(c1.Literals);

            var b2 = a.Literals["b2"];
            Assert.Null(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches));
            Assert.Null(c2.Parameters);
            Assert.Null(c2.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndParameter()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{b}/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Null(a.Matches);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Null(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c1.Parameters);
            Assert.Null(c1.Literals);

            var b2 = a.Parameters;
            Assert.Null(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches));
            Assert.Null(c2.Parameters);
            Assert.Null(c2.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_ParameterAndParameter()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/{b1}/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{b2}/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Null(a.Matches);
            Assert.Null(a.Literals);

            var b = a.Parameters;
            Assert.Null(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Collection(
                c.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c.Parameters);
            Assert.Null(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndCatchAll()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{*b}");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches));

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b1 = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches));
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c1.Parameters);
            Assert.Null(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches));
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_ParameterAndCatchAll()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/{b}/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{*b}");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches));
            Assert.Null(a.Literals);

            var b1 = a.Parameters;
            Assert.Same(endpoint2, Assert.Single(a.Matches));
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e),
                e => Assert.Same(endpoint2, e));
            Assert.Null(c1.Parameters);
            Assert.Null(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches));
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), new TestMetadata2(true), });
            builder.AddEndpoint(endpoint1);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(0, e.Key));

            var test1_0 = a.PolicyEdges[0];
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(test1_0.NodeBuilder);
            Assert.Collection(
                test1_0.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(true, e.Key));

            var test2_true = test1_0.PolicyEdges[true];
            Assert.Same(endpoint1, Assert.Single(test2_true.Matches));
            Assert.Null(test2_true.NodeBuilder);
            Assert.Null(test2_true.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), new TestMetadata2(true), });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), new TestMetadata2(true), });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), new TestMetadata2(false), });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(0, e.Key),
                e => Assert.Equal(1, e.Key));

            var test1_0 = a.PolicyEdges[0];
            Assert.Empty(test1_0.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(test1_0.NodeBuilder);
            Assert.Collection(
                test1_0.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(true, e.Key));

            var test2_true = test1_0.PolicyEdges[true];
            Assert.Same(endpoint1, Assert.Single(test2_true.Matches));
            Assert.Null(test2_true.NodeBuilder);
            Assert.Null(test2_true.PolicyEdges);

            var test1_1 = a.PolicyEdges[1];
            Assert.Empty(test1_1.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(test1_1.NodeBuilder);
            Assert.Collection(
                test1_1.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(false, e.Key),
                e => Assert.Equal(true, e.Key));

            test2_true = test1_1.PolicyEdges[true];
            Assert.Same(endpoint2, Assert.Single(test2_true.Matches));
            Assert.Null(test2_true.NodeBuilder);
            Assert.Null(test2_true.PolicyEdges);

            var test2_false = test1_1.PolicyEdges[false];
            Assert.Same(endpoint3, Assert.Single(test2_false.Matches));
            Assert.Null(test2_false.NodeBuilder);
            Assert.Null(test2_false.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches_FirstPolicySkipped()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(true), });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(true), });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata2(false), });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata2MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(false, e.Key),
                e => Assert.Equal(true, e.Key));

            var test2_true = a.PolicyEdges[true];
            Assert.Equal(new[] { endpoint1, endpoint2, }, test2_true.Matches);
            Assert.Null(test2_true.NodeBuilder);
            Assert.Null(test2_true.PolicyEdges);

            var test2_false = a.PolicyEdges[false];
            Assert.Equal(new[] { endpoint3, }, test2_false.Matches);
            Assert.Null(test2_false.NodeBuilder);
            Assert.Null(test2_false.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches_SecondSkipped()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestMetadata1MatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(0, e.Key),
                e => Assert.Equal(1, e.Key));

            var test1_0 = a.PolicyEdges[0];
            Assert.Equal(new[] { endpoint1, }, test1_0.Matches);
            Assert.Null(test1_0.NodeBuilder);
            Assert.Null(test1_0.PolicyEdges);

            var test1_1 = a.PolicyEdges[1];
            Assert.Equal(new[] { endpoint2, endpoint3, }, test1_1.Matches);
            Assert.Null(test1_1.NodeBuilder);
            Assert.Null(test1_1.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches_NonRouteEndpoint()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestNonRoutePatternMatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(0), });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { new TestMetadata1(1), });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.IsType<TestNonRoutePatternMatcherPolicy>(a.NodeBuilder);
            Assert.Collection(
                a.PolicyEdges.OrderBy(e => e.Key),
                e => Assert.Equal(0, e.Key),
                e => Assert.Equal(1, e.Key),
                e => Assert.Equal(int.MaxValue, e.Key));

            var test1_0 = a.PolicyEdges[0];
            Assert.Equal(new[] { endpoint1, }, test1_0.Matches);
            Assert.Null(test1_0.NodeBuilder);
            Assert.Null(test1_0.PolicyEdges);

            var test1_1 = a.PolicyEdges[1];
            Assert.Equal(new[] { endpoint2, endpoint3, }, test1_1.Matches);
            Assert.Null(test1_1.NodeBuilder);
            Assert.Null(test1_1.PolicyEdges);

            var nonRouteEndpoint = a.PolicyEdges[int.MaxValue];
            Assert.Equal("MaxValueEndpoint", Assert.Single(nonRouteEndpoint.Matches).DisplayName);
        }

        [Fact]
        public void BuildDfaTree_WithPolicies_AndBranches_BothPoliciesSkipped()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            var endpoint1 = CreateEndpoint("/a", metadata: new object[] { });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("/a", metadata: new object[] { });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateEndpoint("/a", metadata: new object[] { });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Equal(new[] { endpoint1, endpoint2, endpoint3, }, a.Matches);
            Assert.Null(a.NodeBuilder);
            Assert.Null(a.PolicyEdges);
        }

        [Fact]
        public void BuildDfaTree_RequiredValues()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint("{controller}/{action}", requiredValues: new { controller = "Home", action = "Index" });
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("Home", next.Key);

            var home = next.Value;
            Assert.Null(home.Matches);
            Assert.Null(home.Parameters);

            next = Assert.Single(home.Literals);
            Assert.Equal("Index", next.Key);

            var index = next.Value;
            Assert.Same(endpoint, Assert.Single(index.Matches));
            Assert.Null(index.Literals);
        }

        [Fact]
        public void BuildDfaTree_RequiredValues_AndMatchingDefaults()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint(
                "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" },
                requiredValues: new { controller = "Home", action = "Index" });
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches));
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("Home", next.Key);

            var home = next.Value;
            Assert.Same(endpoint, Assert.Single(home.Matches));
            Assert.Null(home.Parameters);

            next = Assert.Single(home.Literals);
            Assert.Equal("Index", next.Key);

            var index = next.Value;
            Assert.Same(endpoint, Assert.Single(index.Matches));
            Assert.Null(index.Literals);
        }

        [Fact]
        public void BuildDfaTree_RequiredValues_AndDifferentDefaults()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateSubsitutedEndpoint(
                "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" },
                requiredValues: new { controller = "Login", action = "Index" });
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("Login", next.Key);

            var login = next.Value;
            Assert.Same(endpoint, Assert.Single(login.Matches));
            Assert.Null(login.Parameters);

            next = Assert.Single(login.Literals);
            Assert.Equal("Index", next.Key);

            var index = next.Value;
            Assert.Same(endpoint, Assert.Single(index.Matches));
            Assert.Null(index.Literals);
        }

        [Fact]
        public void BuildDfaTree_RequiredValues_Multiple()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint1 = CreateSubsitutedEndpoint(
                "{controller}/{action}/{id?}",
                defaults: new { controller = "Home", action = "Index" },
                requiredValues: new { controller = "Home", action = "Index" });
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateSubsitutedEndpoint(
                "{controller}/{action}/{id?}",
                defaults: new { controller = "Home", action = "Index" },
                requiredValues: new { controller = "Login", action = "Index" });
            builder.AddEndpoint(endpoint2);

            var endpoint3 = CreateSubsitutedEndpoint(
                "{controller}/{action}/{id?}",
                defaults: new { controller = "Home", action = "Index" },
                requiredValues: new { controller = "Login", action = "ChangePassword" });
            builder.AddEndpoint(endpoint3);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint1, Assert.Single(root.Matches));
            Assert.Null(root.Parameters);

            Assert.Equal(2, root.Literals.Count);

            var home = root.Literals["Home"];

            Assert.Same(endpoint1, Assert.Single(home.Matches));
            Assert.Null(home.Parameters);

            var next = Assert.Single(home.Literals);
            Assert.Equal("Index", next.Key);

            var homeIndex = next.Value;
            Assert.Same(endpoint1, Assert.Single(homeIndex.Matches));
            Assert.Null(homeIndex.Literals);
            Assert.NotNull(homeIndex.Parameters);

            Assert.Same(endpoint1, Assert.Single(homeIndex.Parameters.Matches));

            var login = root.Literals["Login"];

            Assert.Same(endpoint2, Assert.Single(login.Matches));
            Assert.Null(login.Parameters);

            Assert.Equal(2, login.Literals.Count);

            var loginIndex = login.Literals["Index"];

            Assert.Same(endpoint2, Assert.Single(loginIndex.Matches));
            Assert.Null(loginIndex.Literals);
            Assert.NotNull(loginIndex.Parameters);

            Assert.Same(endpoint2, Assert.Single(loginIndex.Parameters.Matches));

            var loginChangePassword = login.Literals["ChangePassword"];

            Assert.Same(endpoint3, Assert.Single(loginChangePassword.Matches));
            Assert.Null(loginChangePassword.Literals);
            Assert.NotNull(loginChangePassword.Parameters);

            Assert.Same(endpoint3, Assert.Single(loginChangePassword.Parameters.Matches));
        }

        [Fact]
        public void BuildDfaTree_RequiredValues_AndParameterTransformer()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint(
                "{controller:slugify}/{action:slugify}",
                defaults: new { controller = "RecentProducts", action = "ViewAll" },
                requiredValues: new { controller = "RecentProducts", action = "ViewAll" });
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches));
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("recent-products", next.Key);

            var home = next.Value;
            Assert.Same(endpoint, Assert.Single(home.Matches));
            Assert.Null(home.Parameters);

            next = Assert.Single(home.Literals);
            Assert.Equal("view-all", next.Key);

            var index = next.Value;
            Assert.Same(endpoint, Assert.Single(index.Matches));
            Assert.Null(index.Literals);
        }

        [Fact]
        public void BuildDfaTree_RequiredValues_AndDefaults_AndParameterTransformer()
        {
            // Arrange
            var builder = CreateDfaMatcherBuilder();

            var endpoint = CreateEndpoint(
                "ConventionalTransformerRoute/{controller:slugify}/{action=Index}/{param:slugify?}",
                requiredValues: new { controller = "ConventionalTransformer", action = "Index", area = (string)null, page = (string)null });
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Null(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("ConventionalTransformerRoute", next.Key);

            var conventionalTransformerRoute = next.Value;
            Assert.Null(conventionalTransformerRoute.Matches);
            Assert.Null(conventionalTransformerRoute.Parameters);

            next = Assert.Single(conventionalTransformerRoute.Literals);
            Assert.Equal("conventional-transformer", next.Key);

            var conventionalTransformer = next.Value;
            Assert.Same(endpoint, Assert.Single(conventionalTransformer.Matches));

            next = Assert.Single(conventionalTransformer.Literals);
            Assert.Equal("Index", next.Key);

            var index = next.Value;
            Assert.Same(endpoint, Assert.Single(index.Matches));

            Assert.NotNull(index.Parameters);

            Assert.Same(endpoint, Assert.Single(index.Parameters.Matches));
        }

        [Fact]
        public void CreateCandidate_JustLiterals()
        {
            // Arrange
            var endpoint = CreateEndpoint("/a/b/c");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(Candidate.CandidateFlags.None, candidate.Flags);
            Assert.Empty(candidate.Slots);
            Assert.Empty(candidate.Captures);
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.Constraints);
        }

        [Fact]
        public void CreateCandidate_Parameters()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{c}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(Candidate.CandidateFlags.HasCaptures, candidate.Flags);
            Assert.Equal(3, candidate.Slots.Length);
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 0), c),
                c => Assert.Equal(("b", 1, 1), c),
                c => Assert.Equal(("c", 2, 2), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.Constraints);
        }

        [Fact]
        public void CreateCandidate_Parameters_WithDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a=aa}/{b=bb}/{c=cc}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults | Candidate.CandidateFlags.HasCaptures,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("a", "aa"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("b", "bb"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 0), c),
                c => Assert.Equal(("b", 1, 1), c),
                c => Assert.Equal(("c", 2, 2), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.Constraints);
        }

        [Fact]
        public void CreateCandidate_Parameters_CatchAll()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{*c=cc}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults |
                    Candidate.CandidateFlags.HasCaptures |
                    Candidate.CandidateFlags.HasCatchAll,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 1), c),
                c => Assert.Equal(("b", 1, 2), c));
            Assert.Equal(("c", 2, 0), candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.Constraints);
        }

        // Defaults are processed first, which affects the slot ordering.
        [Fact]
        public void CreateCandidate_Parameters_OutOfLineDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{c=cc}", new { a = "aa", d = "dd", });

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults | Candidate.CandidateFlags.HasCaptures,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("a", "aa"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("d", "dd"), s),
                s => Assert.Equal(new KeyValuePair<string, object>("c", "cc"), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("a", 0, 0), c),
                c => Assert.Equal(("b", 1, 3), c),
                c => Assert.Equal(("c", 2, 2), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.Constraints);
        }

        [Fact]
        public void CreateCandidate_Parameters_ComplexSegments()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}-{b=bb}/{c}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(
                Candidate.CandidateFlags.HasDefaults |
                    Candidate.CandidateFlags.HasCaptures |
                    Candidate.CandidateFlags.HasComplexSegments,
                candidate.Flags);
            Assert.Collection(
                candidate.Slots,
                s => Assert.Equal(new KeyValuePair<string, object>("b", "bb"), s),
                s => Assert.Equal(new KeyValuePair<string, object>(null, null), s));
            Assert.Collection(
                candidate.Captures,
                c => Assert.Equal(("c", 1, 1), c));
            Assert.Equal(default, candidate.CatchAll);
            Assert.Collection(
                candidate.ComplexSegments,
                s => Assert.Equal(0, s.segmentIndex));
            Assert.Empty(candidate.Constraints);
        }

        [Fact]
        public void CreateCandidate_RouteConstraints()
        {
            // Arrange
            var endpoint = CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), });

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(Candidate.CandidateFlags.HasConstraints, candidate.Flags);
            Assert.Empty(candidate.Slots);
            Assert.Empty(candidate.Captures);
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Single(candidate.Constraints);
        }

        [Fact]
        public void CreateCandidate_CustomParameterPolicy()
        {
            // Arrange
            var endpoint = CreateEndpoint("/a/b/c", constraints: new { a = new CustomParameterPolicy(), });

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(endpoint, score: 0);

            // Assert
            Assert.Equal(Candidate.CandidateFlags.None, candidate.Flags);
            Assert.Empty(candidate.Slots);
            Assert.Empty(candidate.Captures);
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.Constraints);
        }

        private class CustomParameterPolicy : IParameterPolicy
        {
        }

        [Fact]
        public void CreateCandidates_CreatesScoresCorrectly()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new AlphaRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata1(), }),
                CreateEndpoint("/a/b/c", constraints: new { a = new IntRouteConstraint(), }, metadata: new object[] { new TestMetadata2(), }),
                CreateEndpoint("/a/b/c", constraints: new { }, metadata: new object[] { }),
                CreateEndpoint("/a/b/c", constraints: new { }, metadata: new object[] { }),
            };

            var builder = CreateDfaMatcherBuilder(new TestMetadata1MatcherPolicy(), new TestMetadata2MatcherPolicy());

            // Act
            var candidates = builder.CreateCandidates(endpoints);

            // Assert
            Assert.Collection(
                candidates,
                c => Assert.Equal(0, c.Score),
                c => Assert.Equal(0, c.Score),
                c => Assert.Equal(1, c.Score),
                c => Assert.Equal(2, c.Score),
                c => Assert.Equal(3, c.Score),
                c => Assert.Equal(3, c.Score));
        }

        private static DfaMatcherBuilder CreateDfaMatcherBuilder(params MatcherPolicy[] policies)
        {
            var policyFactory = CreateParameterPolicyFactory();
            var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
            return new DfaMatcherBuilder(
                NullLoggerFactory.Instance,
                policyFactory,
                Mock.Of<EndpointSelector>(),
                policies);
        }

        private static RouteEndpoint CreateSubsitutedEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            object requiredValues = null,
            params object[] metadata)
        {
            var routePattern = RoutePatternFactory.Parse(template, defaults, constraints);

            var policyFactory = CreateParameterPolicyFactory();
            var defaultRoutePatternTransformer = new DefaultRoutePatternTransformer(policyFactory);

            routePattern = defaultRoutePatternTransformer.SubstituteRequiredValues(routePattern, requiredValues);

            return EndpointFactory.CreateRouteEndpoint(routePattern, metadata: metadata);
        }

        public static RoutePattern CreateRoutePattern(RoutePattern routePattern, object requiredValues)
        {
            if (requiredValues != null)
            {
                var policyFactory = CreateParameterPolicyFactory();
                var defaultRoutePatternTransformer = new DefaultRoutePatternTransformer(policyFactory);

                routePattern = defaultRoutePatternTransformer.SubstituteRequiredValues(routePattern, requiredValues);
            }

            return routePattern;
        }

        private static DefaultParameterPolicyFactory CreateParameterPolicyFactory()
        {
            var serviceCollection = new ServiceCollection();
            var policyFactory = new DefaultParameterPolicyFactory(
                Options.Create(new RouteOptions
                {
                    ConstraintMap =
                    {
                        ["slugify"] = typeof(SlugifyParameterTransformer),
                        ["upper-case"] = typeof(UpperCaseParameterTransform)
                    }
                }),
                serviceCollection.BuildServiceProvider());

            return policyFactory;
        }

        private static RouteEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            object requiredValues = null,
            params object[] metadata)
        {
            return EndpointFactory.CreateRouteEndpoint(template, defaults, constraints, requiredValues, metadata: metadata);
        }

        private class TestMetadata1
        {
            public TestMetadata1()
            {
            }

            public TestMetadata1(int state)
            {
                State = state;
            }

            public int State { get; set; }
        }

        private class TestMetadata1MatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
        {
            public override int Order => 100;

            public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata1>.Default;

            public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata1>() != null);
            }

            public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints
                    .GroupBy(e => e.Metadata.GetMetadata<TestMetadata1>().State)
                    .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                    .ToArray();
            }
        }

        private class TestMetadata2
        {
            public TestMetadata2()
            {
            }

            public TestMetadata2(bool state)
            {
                State = state;
            }

            public bool State { get; set; }
        }

        private class TestMetadata2MatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
        {
            public override int Order => 101;

            public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata2>.Default;

            public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata2>() != null);
            }

            public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints
                    .GroupBy(e => e.Metadata.GetMetadata<TestMetadata2>().State)
                    .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                    .ToArray();
            }
        }

        private class TestNonRoutePatternMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
        {
            public override int Order => 100;

            public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata1>.Default;

            public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
            {
                return endpoints.Any(e => e.Metadata.GetMetadata<TestMetadata1>() != null);
            }

            public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
            {
                var edges = endpoints
                    .GroupBy(e => e.Metadata.GetMetadata<TestMetadata1>().State)
                    .Select(g => new PolicyNodeEdge(g.Key, g.ToArray()))
                    .ToList();

                var maxValueEndpoint = new Endpoint(TestConstants.EmptyRequestDelegate, EndpointMetadataCollection.Empty, "MaxValueEndpoint");
                edges.Add(new PolicyNodeEdge(int.MaxValue, new[] { maxValueEndpoint }));

                return edges;
            }
        }
    }
}
