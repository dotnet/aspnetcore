// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Diagnostics.Contracts;
using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    // This type is instanciated by frequently called comparison methods so is very performance sensitive
    internal struct ParsedMediaTypeHeaderValue
    {
        private const char MediaRangeAsterisk = '*';
        private const char MediaTypeSubtypeDelimiter = '/';

        private readonly string _mediaType;
        private readonly int _delimiterIndex;
        private readonly bool _isAllMediaRange;
        private readonly bool _isSubtypeMediaRange;

        public ParsedMediaTypeHeaderValue(MediaTypeHeaderValue mediaTypeHeaderValue)
        {
            Contract.Assert(mediaTypeHeaderValue != null);
            string mediaType = _mediaType = mediaTypeHeaderValue.MediaType;
            _delimiterIndex = mediaType.IndexOf(MediaTypeSubtypeDelimiter);
            Contract.Assert(_delimiterIndex > 0, "The constructor of the MediaTypeHeaderValue would have failed if there wasn't a type and subtype.");

            _isAllMediaRange = false;
            _isSubtypeMediaRange = false;
            int mediaTypeLength = mediaType.Length;
            if (_delimiterIndex == mediaTypeLength - 2)
            {
                if (mediaType[mediaTypeLength - 1] == MediaRangeAsterisk)
                {
                    _isSubtypeMediaRange = true;
                    if (_delimiterIndex == 1 && mediaType[0] == MediaRangeAsterisk)
                    {
                        _isAllMediaRange = true;
                    }
                }
            }
        }

        public bool IsAllMediaRange
        {
            get { return _isAllMediaRange; }
        }

        public bool IsSubtypeMediaRange
        {
            get { return _isSubtypeMediaRange; }
        }

        public bool TypesEqual(ref ParsedMediaTypeHeaderValue other)
        {
            if (_delimiterIndex != other._delimiterIndex)
            {
                return false;
            }
            return String.Compare(_mediaType, 0, other._mediaType, 0, _delimiterIndex, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public bool SubTypesEqual(ref ParsedMediaTypeHeaderValue other)
        {
            int _subTypeLength = _mediaType.Length - _delimiterIndex - 1;
            if (_subTypeLength != other._mediaType.Length - other._delimiterIndex - 1)
            {
                return false;
            }
            return String.Compare(_mediaType, _delimiterIndex + 1, other._mediaType, other._delimiterIndex + 1, _subTypeLength, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
#endif