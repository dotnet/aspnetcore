// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.Net.Http.Headers
{
    internal sealed class GenericHeaderParser<T> : BaseHeaderParser<T>
    {
        internal delegate int GetParsedValueLengthDelegate(string value, int startIndex, out T parsedValue);

        private GetParsedValueLengthDelegate _getParsedValueLength;

        internal GenericHeaderParser(bool supportsMultipleValues, [NotNull] GetParsedValueLengthDelegate getParsedValueLength)
            : base(supportsMultipleValues)
        {
            _getParsedValueLength = getParsedValueLength;
        }

        protected override int GetParsedValueLength(string value, int startIndex, out T parsedValue)
        {
            return _getParsedValueLength(value, startIndex, out parsedValue);
        }
    }
}
