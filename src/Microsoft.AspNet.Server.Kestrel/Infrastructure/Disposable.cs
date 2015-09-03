// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for Disposable
    /// </summary>
    public class Disposable : IDisposable
    {
        private Action _dispose;
        private bool disposedValue = false; // To detect redundant calls

        public Disposable(Action dispose)
        {
            _dispose = dispose;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _dispose.Invoke();
                }

                _dispose = null;
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}