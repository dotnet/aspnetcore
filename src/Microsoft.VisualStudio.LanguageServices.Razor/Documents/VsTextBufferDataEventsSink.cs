// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class VsTextBufferDataEventsSink : IVsTextBufferDataEvents
    {
        private readonly Action _action;
        private readonly IConnectionPoint _connectionPoint;
        private uint _cookie;
        
        public static void Subscribe(IVsTextBuffer vsTextBuffer, Action action)
        {
            if (vsTextBuffer == null)
            {
                throw new ArgumentNullException(nameof(vsTextBuffer));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var connectionPointContainer = (IConnectionPointContainer)vsTextBuffer;

            var guid = typeof(IVsTextBufferDataEvents).GUID;
            connectionPointContainer.FindConnectionPoint(ref guid, out var connectionPoint);

            var sink = new VsTextBufferDataEventsSink(connectionPoint, action);
            connectionPoint.Advise(sink, out sink._cookie);
        }

        private VsTextBufferDataEventsSink(IConnectionPoint connectionPoint, Action action)
        {
            _connectionPoint = connectionPoint;
            _action = action;
        }

        public void OnFileChanged(uint grfChange, uint dwFileAttrs)
        {
            // ignore
        }

        public int OnLoadCompleted(int fReload)
        {
            _connectionPoint.Unadvise(_cookie);
            _action();

            return VSConstants.S_OK;
        }
    }
}
