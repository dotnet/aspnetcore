// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    internal class ForegroundFactTestCase : LongLivedMarshalByRefObject, IXunitTestCase
    {
        private IXunitTestCase _inner;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer", error: true)]
        public ForegroundFactTestCase()
        {
        }

        public ForegroundFactTestCase(IXunitTestCase testCase)
        {
            _inner = testCase;
        }

        public string DisplayName => _inner.DisplayName;

        public IMethodInfo Method => _inner.Method;

        public string SkipReason => _inner.SkipReason;

        public ISourceInformation SourceInformation
        {
            get => _inner.SourceInformation;
            set => _inner.SourceInformation = value;
        }

        public ITestMethod TestMethod => _inner.TestMethod;

        public object[] TestMethodArguments => _inner.TestMethodArguments;

        public Dictionary<string, List<string>> Traits => _inner.Traits;

        public string UniqueID => _inner.UniqueID;

        public void Deserialize(IXunitSerializationInfo info)
        {
            _inner = info.GetValue<IXunitTestCase>("InnerTestCase");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("InnerTestCase", _inner);
        }

        public Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            var tcs = new TaskCompletionSource<RunSummary>();
            var thread = new Thread(() =>
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                    
                    var worker = _inner.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);

                    Exception caught = null;
                    var frame = new DispatcherFrame();
                    Task.Run(async () =>
                    {
                        try
                        {
                            await worker;
                        }
                        catch (Exception ex)
                        {
                            caught = ex;
                        }
                        finally
                        {
                            frame.Continue = false;
                        }
                    });

                    Dispatcher.PushFrame(frame);

                    if (caught == null)
                    {
                        tcs.SetResult(worker.Result);
                    }
                    else
                    { 
                        tcs.SetException(caught);
                    }
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }
    }
}
