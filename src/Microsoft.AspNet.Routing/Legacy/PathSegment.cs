// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.Routing.Legacy
{
    // Represents a segment of a URI such as a separator or content
    internal abstract class PathSegment
    {
#if ROUTE_DEBUGGING
        public abstract string LiteralText
        {
            get;
        }
#endif
    }

    // Represents a segment of a URI that is not a separator. It contains subsegments such as literals and parameters.
    internal sealed class PathContentSegment : PathSegment
    {
        public PathContentSegment(List<PathSubsegment> subsegments)
        {
            Subsegments = subsegments;
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Not changing original algorithm.")]
        public bool IsCatchAll
        {
            get
            {
                // TODO: Verify this is correct. Maybe add an assert.
                // Performance sensitive
                // Caching count is faster for IList<T>
                int subsegmentCount = Subsegments.Count;
                for (int i = 0; i < subsegmentCount; i++)
                {
                    PathSubsegment seg = Subsegments[i];
                    PathParameterSubsegment paramterSubSegment = seg as PathParameterSubsegment;
                    if (paramterSubSegment != null && paramterSubSegment.IsCatchAll)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public List<PathSubsegment> Subsegments { get; private set; }

#if ROUTE_DEBUGGING
        public override string LiteralText
        {
            get
            {
                List<string> s = new List<string>();
                foreach (PathSubsegment subsegment in Subsegments)
                {
                    s.Add(subsegment.LiteralText);
                }
                return String.Join(String.Empty, s.ToArray());
            }
        }

        public override string ToString()
        {
            List<string> s = new List<string>();
            foreach (PathSubsegment subsegment in Subsegments)
            {
                s.Add(subsegment.ToString());
            }
            return "[ " + String.Join(", ", s.ToArray()) + " ]";
        }
#endif
    }

    // Represents a literal subsegment of a ContentPathSegment
    internal sealed class PathLiteralSubsegment : PathSubsegment
    {
        public PathLiteralSubsegment(string literal)
        {
            Literal = literal;
        }

        public string Literal { get; private set; }

#if ROUTE_DEBUGGING
        public override string LiteralText
        {
            get
            {
                return Literal;
            }
        }

        public override string ToString()
        {
            return "\"" + Literal + "\"";
        }
#endif
    }

    // Represents a parameter subsegment of a ContentPathSegment
    internal sealed class PathParameterSubsegment : PathSubsegment
    {
        public PathParameterSubsegment(string parameterName)
        {
            if (parameterName.StartsWith("*", StringComparison.Ordinal))
            {
                ParameterName = parameterName.Substring(1);
                IsCatchAll = true;
            }
            else
            {
                ParameterName = parameterName;
            }
        }

        public bool IsCatchAll { get; private set; }

        public string ParameterName { get; private set; }

#if ROUTE_DEBUGGING
        public override string LiteralText
        {
            get
            {
                return "{" + (IsCatchAll ? "*" : String.Empty) + ParameterName + "}";
            }
        }

        public override string ToString()
        {
            return "{" + (IsCatchAll ? "*" : String.Empty) + ParameterName + "}";
        }
#endif
    }

    // Represents a "/" separator in a URI
    internal sealed class PathSeparatorSegment : PathSegment
    {
#if ROUTE_DEBUGGING
        public override string LiteralText
        {
            get
            {
                return "/";
            }
        }

        public override string ToString()
        {
            return "\"/\"";
        }
#endif
    }

    // Represents a subsegment of a ContentPathSegment such as a parameter or a literal.
    internal abstract class PathSubsegment
    {
#if ROUTE_DEBUGGING
        public abstract string LiteralText
        {
            get;
        }
#endif
    }
}
