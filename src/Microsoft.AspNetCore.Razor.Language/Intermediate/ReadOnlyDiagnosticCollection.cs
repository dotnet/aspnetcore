// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class ReadOnlyDiagnosticCollection : RazorDiagnosticCollection
    {
        public static readonly ReadOnlyDiagnosticCollection Instance = new ReadOnlyDiagnosticCollection();

        private ReadOnlyDiagnosticCollection()
        {
        }

        public override RazorDiagnostic this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                throw null; // Unreachable
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                throw null; // Unreachable
            }
        }

        public override int Count => 0;

        public override bool IsReadOnly => true;

        public override void Add(RazorDiagnostic item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            throw new NotSupportedException();
        }

        public override void Clear()
        {
            throw new NotSupportedException();
        }

        public override bool Contains(RazorDiagnostic item)
        {
            return false;
        }

        public override void CopyTo(RazorDiagnostic[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            else if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            throw new NotSupportedException();
        }

        public override IEnumerator<RazorDiagnostic> GetEnumerator()
        {
            return Enumerable.Empty<RazorDiagnostic>().GetEnumerator();
        }

        public override int IndexOf(RazorDiagnostic item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return -1;
        }

        public override void Insert(int index, RazorDiagnostic item)
        {
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            throw new NotSupportedException();
        }

        public override bool Remove(RazorDiagnostic item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return false;
        }

        public override void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            throw new NotSupportedException();
        }
    }
}
