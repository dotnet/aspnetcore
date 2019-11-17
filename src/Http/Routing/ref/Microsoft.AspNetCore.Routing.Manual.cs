// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal abstract partial class Matcher
    {
        protected Matcher() { }
        public abstract System.Threading.Tasks.Task MatchAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
    internal abstract partial class MatcherFactory
    {
        protected MatcherFactory() { }
        public abstract Microsoft.AspNetCore.Routing.Matching.Matcher CreateMatcher(Microsoft.AspNetCore.Routing.EndpointDataSource dataSource);
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct PathSegment : System.IEquatable<Microsoft.AspNetCore.Routing.Matching.PathSegment>
    {
        private readonly int _dummyPrimitive;
        public PathSegment(int start, int length) { throw null; }
        public bool Equals(Microsoft.AspNetCore.Routing.Matching.PathSegment other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
}
