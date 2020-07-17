// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class SourceMapping : IEquatable<SourceMapping>
    {
        public SourceMapping(SourceSpan originalSpan, SourceSpan generatedSpan)
        {
            OriginalSpan = originalSpan;
            GeneratedSpan = generatedSpan;
        }

        public SourceSpan OriginalSpan { get; }

        public SourceSpan GeneratedSpan { get; }

        public override bool Equals(object obj)
        {
            var other = obj as SourceMapping;
            return Equals(other);
        }

        public bool Equals(SourceMapping other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return OriginalSpan.Equals(other.OriginalSpan) &&
                GeneratedSpan.Equals(other.GeneratedSpan);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(OriginalSpan);
            hashCodeCombiner.Add(GeneratedSpan);

            return hashCodeCombiner;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} -> {1}", OriginalSpan, GeneratedSpan);
        }
    }
}
