// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
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
            Assert.Same(endpoint, Assert.Single(root.Matches).Endpoint);
            Assert.Null(root.Parameters);
            Assert.Empty(root.Literals);
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
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.Null(a.Parameters);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Same(endpoint, Assert.Single(c.Matches).Endpoint);
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
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
            Assert.Empty(root.Matches);
            Assert.Empty(root.Literals);

            var a = root.Parameters;
            Assert.Empty(a.Matches);
            Assert.Empty(a.Literals);

            var b = a.Parameters;
            Assert.Empty(b.Matches);
            Assert.Empty(b.Literals);

            var c = b.Parameters;
            Assert.Same(endpoint, Assert.Single(c.Matches).Endpoint);
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
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
            Assert.Empty(root.Matches);
            Assert.Empty(root.Literals);

            var a = root.Parameters;

            // The catch all can match a path like '/a'
            Assert.Same(endpoint, Assert.Single(a.Matches).Endpoint);
            Assert.Empty(a.Literals);
            Assert.Null(a.Parameters);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = a.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches).Endpoint);
            Assert.Empty(catchAll.Literals);
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
            Assert.Same(endpoint, Assert.Single(root.Matches).Endpoint);
            Assert.Empty(root.Literals);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = root.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches).Endpoint);
            Assert.Empty(catchAll.Literals);
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
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);

            Assert.Equal(2, a.Literals.Count);

            var b1 = a.Literals["b1"];
            Assert.Empty(b1.Matches);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Same(endpoint1, Assert.Single(c1.Matches).Endpoint);
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var b2 = a.Literals["b2"];
            Assert.Empty(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches).Endpoint);
            Assert.Null(c2.Parameters);
            Assert.Empty(c2.Literals);
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
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var b2 = a.Parameters;
            Assert.Empty(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches).Endpoint);
            Assert.Null(c2.Parameters);
            Assert.Empty(c2.Literals);
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
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.Empty(a.Literals);

            var b = a.Parameters;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Collection(
                c.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
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
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b1 = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches).Endpoint);
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
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);
            Assert.Empty(a.Literals);

            var b1 = a.Parameters;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches).Endpoint);
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void CreateCandidate_JustLiterals()
        {
            // Arrange
            var endpoint = CreateEndpoint("/a/b/c");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(new MatcherBuilderEntry(endpoint));

            // Assert
            Assert.Equal(Candidate.CandidateFlags.None, candidate.Flags);
            Assert.Empty(candidate.Slots);
            Assert.Empty(candidate.Captures);
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{c}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(new MatcherBuilderEntry(endpoint));

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
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters_WithDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a=aa}/{b=bb}/{c=cc}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(new MatcherBuilderEntry(endpoint));

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
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters_CatchAll()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{*c=cc}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(new MatcherBuilderEntry(endpoint));

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
            Assert.Empty(candidate.MatchProcessors);
        }

        // Defaults are processed first, which affects the slot ordering.
        [Fact]
        public void CreateCandidate_Parameters_OutOfLineDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}/{b}/{c=cc}", new { a = "aa", d = "dd", });

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(new MatcherBuilderEntry(endpoint));

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
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_Parameters_ComplexSegments()
        {
            // Arrange
            var endpoint = CreateEndpoint("/{a}-{b=bb}/{c}");

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(new MatcherBuilderEntry(endpoint));

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
            Assert.Empty(candidate.MatchProcessors);
        }

        [Fact]
        public void CreateCandidate_MatchProcessors()
        {
            // Arrange
            var endpoint = CreateEndpoint("/a/b/c", matchProcessors: new MatchProcessorReference[]
            {
                new MatchProcessorReference("a", new IntRouteConstraint()),
            });

            var builder = CreateDfaMatcherBuilder();

            // Act
            var candidate = builder.CreateCandidate(new MatcherBuilderEntry(endpoint));

            // Assert
            Assert.Equal( Candidate.CandidateFlags.HasMatchProcessors, candidate.Flags);
            Assert.Empty(candidate.Slots);
            Assert.Empty(candidate.Captures);
            Assert.Equal(default, candidate.CatchAll);
            Assert.Empty(candidate.ComplexSegments);
            Assert.Single(candidate.MatchProcessors);
        }

        private static DfaMatcherBuilder CreateDfaMatcherBuilder()
        {
            var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
            return new DfaMatcherBuilder(
                Mock.Of<MatchProcessorFactory>(),
                new EndpointSelector(
                    dataSource,
                    new EndpointConstraintCache(dataSource, Array.Empty<IEndpointConstraintProvider>()),
                    NullLoggerFactory.Instance));
        }

        private MatcherEndpoint CreateEndpoint(
            string template, 
            object defaults = null,
            IEnumerable<MatchProcessorReference> matchProcessors = null)
        {
            matchProcessors = matchProcessors ?? Array.Empty<MatchProcessorReference>();

            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(),
                matchProcessors.ToList(),
                0,
                new EndpointMetadataCollection(Array.Empty<object>()),
                "test");
        }
    }
}
