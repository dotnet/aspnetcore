// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    internal sealed class GenericHeaderParser<T> : BaseHeaderParser<T>
    {
        internal delegate int GetParsedValueLengthDelegate(StringSegment value, int startIndex, out T? parsedValue);

        private GetParsedValueLengthDelegate _getParsedValueLength;

        internal GenericHeaderParser(bool supportsMultipleValues, GetParsedValueLengthDelegate getParsedValueLength)
            : base(supportsMultipleValues)
        {
            if (getParsedValueLength == null)
            {
                throw new ArgumentNullException(nameof(getParsedValueLength));
            }

            _getParsedValueLength = getParsedValueLength;
        }

        protected override int GetParsedValueLength(StringSegment value, int startIndex, out T? parsedValue)
        {
            return _getParsedValueLength(value, startIndex, out parsedValue);
        }
    }
}
