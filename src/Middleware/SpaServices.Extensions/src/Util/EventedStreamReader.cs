// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.Util
{
    /// <summary>
    /// Wraps a <see cref="StreamReader"/> to expose an evented API, issuing notifications
    /// when the stream emits partial lines, completed lines, or finally closes.
    /// </summary>
    internal class EventedStreamReader
    {
        public delegate void OnReceivedChunkHandler(ArraySegment<char> chunk);
        public delegate void OnReceivedLineHandler(string line);
        public delegate void OnStreamClosedHandler();

        public event OnReceivedChunkHandler OnReceivedChunk;
        public event OnReceivedLineHandler OnReceivedLine;
        public event OnStreamClosedHandler OnStreamClosed;

        private readonly StreamReader _streamReader;
        private readonly StringBuilder _linesBuffer;

        public EventedStreamReader(StreamReader streamReader)
        {
            _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
            _linesBuffer = new StringBuilder();
            Task.Factory.StartNew(Run);
        }

        public Task<Match> WaitForMatch(Regex regex)
        {
            var tcs = new TaskCompletionSource<Match>();
            var completionLock = new object();

            OnReceivedLineHandler onReceivedLineHandler = null;
            OnStreamClosedHandler onStreamClosedHandler = null;

            void ResolveIfStillPending(Action applyResolution)
            {
                lock (completionLock)
                {
                    if (!tcs.Task.IsCompleted)
                    {
                        OnReceivedLine -= onReceivedLineHandler;
                        OnStreamClosed -= onStreamClosedHandler;
                        applyResolution();
                    }
                }
            }

            onReceivedLineHandler = line =>
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    ResolveIfStillPending(() => tcs.SetResult(match));
                }
            };

            onStreamClosedHandler = () =>
            {
                ResolveIfStillPending(() => tcs.SetException(new EndOfStreamException()));
            };

            OnReceivedLine += onReceivedLineHandler;
            OnStreamClosed += onStreamClosedHandler;

            return tcs.Task;
        }

        private async Task Run()
        {
            var buf = new char[8 * 1024];
            while (true)
            {
                var chunkLength = await _streamReader.ReadAsync(buf, 0, buf.Length);
                if (chunkLength == 0)
                {
                    OnClosed();
                    break;
                }

                OnChunk(new ArraySegment<char>(buf, 0, chunkLength));

                var lineBreakPos = Array.IndexOf(buf, '\n', 0, chunkLength);
                if (lineBreakPos < 0)
                {
                    _linesBuffer.Append(buf, 0, chunkLength);
                }
                else
                {
                    _linesBuffer.Append(buf, 0, lineBreakPos + 1);
                    OnCompleteLine(_linesBuffer.ToString());
                    _linesBuffer.Clear();
                    _linesBuffer.Append(buf, lineBreakPos + 1, chunkLength - (lineBreakPos + 1));
                }
            }
        }

        private void OnChunk(ArraySegment<char> chunk)
        {
            var dlg = OnReceivedChunk;
            dlg?.Invoke(chunk);
        }

        private void OnCompleteLine(string line)
        {
            var dlg = OnReceivedLine;
            dlg?.Invoke(line);
        }

        private void OnClosed()
        {
            var dlg = OnStreamClosed;
            dlg?.Invoke();
        }
    }
}
