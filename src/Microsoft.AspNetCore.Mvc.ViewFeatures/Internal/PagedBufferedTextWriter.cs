// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PagedBufferedTextWriter : TextWriter
    {
        public const int PageSize = 1024;

        private readonly TextWriter _inner;
        private readonly List<char[]> _pages;
        private readonly ArrayPool<char> _pool;

        private int _currentPage;
        private int _currentIndex; // The next 'free' character

        public PagedBufferedTextWriter(ArrayPool<char> pool, TextWriter inner)
        {
            _pool = pool;
            _inner = inner;
            _pages = new List<char[]>();
        }

        public override Encoding Encoding => _inner.Encoding;

        public override void Flush()
        {
            // Don't do anything. We'll call FlushAsync.
        }

        public override async Task FlushAsync()
        {
            if (_pages.Count == 0)
            {
                return;
            }

            for (var i = 0; i <= _currentPage; i++)
            {
                var page = _pages[i];

                var count = i == _currentPage ? _currentIndex : page.Length;
                if (count > 0)
                {
                    await _inner.WriteAsync(page, 0, count);
                }
            }

            // Return all but one of the pages. This way if someone writes a large chunk of
            // content, we can return those buffers and avoid holding them for the whole
            // page's lifetime.
            for (var i = _pages.Count - 1; i > 0; i--)
            {
                var page = _pages[i];

                try
                {
                    _pages.RemoveAt(i);
                }
                finally
                {
                    _pool.Return(page);
                }
            }

            _currentPage = 0;
            _currentIndex = 0;
        }

        public override void Write(char value)
        {
            var page = GetCurrentPage();
            page[_currentIndex++] = value;
        }

        public override void Write(char[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            Write(buffer, 0, buffer.Length);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            while (count > 0)
            {
                var page = GetCurrentPage();
                var copyLength = Math.Min(count, page.Length - _currentIndex);
                Debug.Assert(copyLength > 0);

                Array.Copy(
                    buffer,
                    index,
                    page,
                    _currentIndex,
                    copyLength);

                _currentIndex += copyLength;
                index += copyLength;

                count -= copyLength;
            }
        }

        public override void Write(string value)
        {
            if (value == null)
            {
                return;
            }

            var index = 0;
            var count = value.Length;

            while (count > 0)
            {
                var page = GetCurrentPage();
                var copyLength = Math.Min(count, page.Length - _currentIndex);
                Debug.Assert(copyLength > 0);

                value.CopyTo(
                    index,
                    page,
                    _currentIndex,
                    copyLength);

                _currentIndex += copyLength;
                index += copyLength;

                count -= copyLength;
            }
        }

        public override Task WriteAsync(char value)
        {
            return _inner.WriteAsync(value);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return _inner.WriteAsync(buffer, index, count);
        }

        public override Task WriteAsync(string value)
        {
            return _inner.WriteAsync(value);
        }

        private char[] GetCurrentPage()
        {
            char[] page = null;
            if (_pages.Count == 0)
            {
                Debug.Assert(_currentPage == 0);
                Debug.Assert(_currentIndex == 0);

                try
                {
                    page = _pool.Rent(PageSize);
                    _pages.Add(page);
                }
                catch when (page != null)
                {
                    _pool.Return(page);
                    throw;
                }

                return page;
            }

            Debug.Assert(_pages.Count > _currentPage);
            page = _pages[_currentPage];

            if (_currentIndex == page.Length)
            {
                // Current page is full.
                _currentPage++;
                _currentIndex = 0;

                if (_pages.Count == _currentPage)
                {
                    try
                    {
                        page = _pool.Rent(PageSize);
                        _pages.Add(page);
                    }
                    catch when (page != null)
                    {
                        _pool.Return(page);
                        throw;
                    }
                }
            }

            return page;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            for (var i = 0; i < _pages.Count; i++)
            {
                _pool.Return(_pages[i]);
            }

            _pages.Clear();
        }
    }
}
