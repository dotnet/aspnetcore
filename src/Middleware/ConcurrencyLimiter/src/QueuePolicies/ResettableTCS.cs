// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    internal class ResettableBooleanTCS : ICriticalNotifyCompletion
    {
        private static readonly Action _callbackCompletedFalse = () => { };
        private static readonly Action _callbackCompletedTrue = () => { };

        private Action _callback;

        public ResettableBooleanTCS GetAwaiter() => this;
        public bool IsCompleted => EqualsEitherCompletionAction(_callback);

        public bool GetResult()
        {
            if (ReferenceEquals(_callback, _callbackCompletedFalse))
            {
                _callback = null;
                return false;
            }
            else if (ReferenceEquals(_callback, _callbackCompletedTrue))
            {
                _callback = null;
                return true;
            }

            throw new InvalidAsynchronousStateException();
        }

        private bool EqualsEitherCompletionAction(object Obj)
        {
            return ReferenceEquals(Obj, _callbackCompletedFalse) || ReferenceEquals(Obj, _callbackCompletedTrue);
        }

        public void OnCompleted(Action continuation)
        {
            // if (we are already done) OR
            //    (if in the action of setting `_callback = continuation` we become done)
            // then { short circuit; run the continuation right away }
            // otherwise { return and the state machine will run the callback at an acceptable time }

            if (EqualsEitherCompletionAction(_callback) ||
                EqualsEitherCompletionAction(Interlocked.CompareExchange(ref _callback, continuation, null)))
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void CompleteTrue()
        {
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompletedTrue);
            continuation?.Invoke();
        }

        public void CompleteFalse()
        {
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompletedFalse);
            continuation?.Invoke();
        }
    }
}
