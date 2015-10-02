// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Routing.Internal
{
    public struct PathSegment : IEquatable<PathSegment>, IEquatable<string>
    {
        private readonly string _path;
        private readonly int _start;
        private readonly int _length;

        private string _segment;

        public PathSegment(string path, int start, int length)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (start < 1 || start >= path.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (length < 0 || start + length > path.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _path = path;
            _start = start;
            _length = length;

            _segment = null;
        }

        public int Length => _length;

        public string GetRemainingPath()
        {
            if (_path == null)
            {
                return string.Empty;
            }

            return _path.Substring(_start);
        }

        public override bool Equals(object obj)
        {
            var other = obj as PathSegment?;
            if (other == null)
            {
                return false;
            }
            else
            {
                return Equals(other.Value);
            }
        }

        public override int GetHashCode()
        {
            if (_path == null)
            {
                return 0;
            }

            return StringComparer.OrdinalIgnoreCase.GetHashCode(ToString());
        }

        public override string ToString()
        {
            if (_path == null)
            {
                return string.Empty;
            }

            if (_segment == null)
            {
                _segment = _path.Substring(_start, _length);
            }

            return _segment;
        }

        public bool Equals(PathSegment other)
        {
            if (_path == null)
            {
                return other._path == null;
            }

            if (other._length != _length)
            {
                return false;
            }

            return string.Compare(
                _path,
                _start,
                other._path,
                other._start,
                _length,
                StringComparison.OrdinalIgnoreCase) == 0;
        }

        public bool Equals(string other)
        {
            if (_path == null)
            {
                return other == null;
            }

            if (other.Length != _length)
            {
                return false;
            }

            return string.Compare(_path, _start, other, 0, _length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool operator ==(PathSegment x, PathSegment y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(PathSegment x, PathSegment y)
        {
            return !x.Equals(y);
        }
    }
}
