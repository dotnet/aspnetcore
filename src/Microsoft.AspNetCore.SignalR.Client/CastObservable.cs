// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Client
{
    internal class CastObservable<TResult> : IObservable<TResult>
    {
        private IObservable<object> _innerObservable;

        public CastObservable(IObservable<object> innerObservable)
        {
            _innerObservable = innerObservable;
        }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            return _innerObservable.Subscribe(new CastObserver(observer));
        }

        private class CastObserver : IObserver<object>
        {
            private IObserver<TResult> _innerObserver;

            public CastObserver(IObserver<TResult> innerObserver)
            {
                _innerObserver = innerObserver;
            }

            public void OnCompleted()
            {
                _innerObserver.OnCompleted();
            }

            public void OnError(Exception error)
            {
                _innerObserver.OnError(error);
            }

            public void OnNext(object value)
            {
                try
                {
                    _innerObserver.OnNext((TResult)value);
                }
                catch(Exception ex)
                {
                    _innerObserver.OnError(ex);
                }
            }
        }
    }
}
