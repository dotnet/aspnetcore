// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public struct ParsedPath
    {
        private static readonly string[] Empty = null;

        private readonly string[] _segments;

        public ParsedPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _segments = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string LastSegment
        {
            get
            {
                if (_segments == null || _segments.Length == 0)
                {
                    return null;
                }

                return _segments[_segments.Length - 1];
            }
        }

        public IReadOnlyList<string> Segments => _segments ?? Empty;
    }
}
