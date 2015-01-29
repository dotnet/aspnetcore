// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Diagnostics;
using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    // This type is instantiated by frequently called comparison methods so is very performance sensitive
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
            Debug.Assert(mediaTypeHeaderValue != null);
            var mediaType = _mediaType = mediaTypeHeaderValue.MediaType;
            _delimiterIndex = mediaType.IndexOf(MediaTypeSubtypeDelimiter);
            Debug.Assert(
                _delimiterIndex > 0,
                "The constructor of the MediaTypeHeaderValue would have failed if there wasn't a type and subtype.");

            _isAllMediaRange = false;
            _isSubtypeMediaRange = false;
            var mediaTypeLength = mediaType.Length;
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

            return string.Compare(
                strA: _mediaType,
                indexA: 0,
                strB: other._mediaType,
                indexB: 0,
                length: _delimiterIndex,
                comparisonType: StringComparison.OrdinalIgnoreCase) == 0;
        }

        public bool SubTypesEqual(ref ParsedMediaTypeHeaderValue other)
        {
            var _subTypeLength = _mediaType.Length - _delimiterIndex - 1;
            if (_subTypeLength != other._mediaType.Length - other._delimiterIndex - 1)
            {
                return false;
            }

            return string.Compare(
                strA: _mediaType,
                indexA: _delimiterIndex + 1,
                strB: other._mediaType,
                indexB: other._delimiterIndex + 1,
                length: _subTypeLength,
                comparisonType: StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
#endif