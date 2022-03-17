// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Pipelines;

public static class PipeReaderExtensions
{
    public static async Task<bool> WaitToReadAsync(this PipeReader pipeReader)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();

            try
            {
                if (!result.Buffer.IsEmpty)
                {
                    return true;
                }

                if (result.IsCompleted)
                {
                    return false;
                }
            }
            finally
            {
                // Don't consume or advance
                pipeReader.AdvanceTo(result.Buffer.Start, result.Buffer.Start);
            }
        }
    }

    public static async Task<byte[]> ReadSingleAsync(this PipeReader pipeReader)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();

            try
            {
                return result.Buffer.ToArray();
            }
            finally
            {
                pipeReader.AdvanceTo(result.Buffer.End);
            }
        }
    }

    public static async Task ConsumeAsync(this PipeReader pipeReader, int numBytes)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();
            if (result.Buffer.Length < numBytes)
            {
                pipeReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                continue;
            }

            pipeReader.AdvanceTo(result.Buffer.GetPosition(numBytes));
            break;
        }
    }

    public static async Task<byte[]> ReadAllAsync(this PipeReader pipeReader)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();

            if (result.IsCompleted)
            {
                return result.Buffer.ToArray();
            }

            // Consume nothing, just wait for everything
            pipeReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        }
    }

    public static async Task<byte[]> ReadAsync(this PipeReader pipeReader, int numBytes)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();
            if (result.Buffer.Length < numBytes)
            {
                pipeReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                continue;
            }

            var buffer = result.Buffer.Slice(0, numBytes);

            var bytes = buffer.ToArray();

            pipeReader.AdvanceTo(buffer.End);

            return bytes;
        }
    }
}
