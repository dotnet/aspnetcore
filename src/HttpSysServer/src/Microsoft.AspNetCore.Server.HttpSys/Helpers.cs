// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class Helpers
    {
        internal static readonly byte[] ChunkTerminator = new byte[] { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        internal static readonly byte[] CRLF = new byte[] { (byte)'\r', (byte)'\n' };

        internal static ConfiguredTaskAwaitable SupressContext(this Task task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        internal static ConfiguredTaskAwaitable<T> SupressContext<T>(this Task<T> task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        internal static IAsyncResult ToIAsyncResult(this Task task, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(0);
                }

                if (callback != null)
                {
                    callback(tcs.Task);
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            return tcs.Task;
        }

        internal static ArraySegment<byte> GetChunkHeader(long size)
        {
            if (size < int.MaxValue)
            {
                return GetChunkHeader((int)size);
            }

            // Greater than 2gb, perf is no longer our concern
            return new ArraySegment<byte>(Encoding.ASCII.GetBytes(size.ToString("X") + "\r\n"));
        }

        /// <summary>
        /// A private utility routine to convert an integer to a chunk header,
        /// which is an ASCII hex number followed by a CRLF.The header is returned
        /// as a byte array.
        /// Generates a right-aligned hex string and returns the start offset.
        /// </summary>
        /// <param name="size">Chunk size to be encoded</param>
        /// <returns>A byte array with the header in int.</returns>
        internal static ArraySegment<byte> GetChunkHeader(int size)
        {
            uint mask = 0xf0000000;
            byte[] header = new byte[10];
            int i;
            int offset = -1;

            // Loop through the size, looking at each nibble. If it's not 0
            // convert it to hex. Save the index of the first non-zero
            // byte.

            for (i = 0; i < 8; i++, size <<= 4)
            {
                // offset == -1 means that we haven't found a non-zero nibble
                // yet. If we haven't found one, and the current one is zero,
                // don't do anything.

                if (offset == -1)
                {
                    if ((size & mask) == 0)
                    {
                        continue;
                    }
                }

                // Either we have a non-zero nibble or we're no longer skipping
                // leading zeros. Convert this nibble to ASCII and save it.

                uint temp = (uint)size >> 28;

                if (temp < 10)
                {
                    header[i] = (byte)(temp + '0');
                }
                else
                {
                    header[i] = (byte)((temp - 10) + 'A');
                }

                // If we haven't found a non-zero nibble yet, we've found one
                // now, so remember that.

                if (offset == -1)
                {
                    offset = i;
                }
            }

            header[8] = (byte)'\r';
            header[9] = (byte)'\n';

            return new ArraySegment<byte>(header, offset, header.Length - offset);
        }
    }
}
