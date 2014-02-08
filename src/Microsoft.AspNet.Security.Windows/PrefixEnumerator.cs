//------------------------------------------------------------------------------
// <copyright file="PrefixEnumerator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Security.Windows
{
    internal class PrefixEnumerator : IEnumerator<string>
    {
        private IEnumerator enumerator;

        internal PrefixEnumerator(IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        public string Current
        {
            get
            {
                return (string)enumerator.Current;
            }
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return enumerator.Current;
            }
        }

        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        public void Dispose()
        {
        }

        void System.Collections.IEnumerator.Reset()
        {
            enumerator.Reset();
        }
    }
}
