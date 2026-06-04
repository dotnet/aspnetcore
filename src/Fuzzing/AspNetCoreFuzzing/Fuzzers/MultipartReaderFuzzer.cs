// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace AspNetCoreFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the MultipartReader which parses multipart form data (RFC 2046)
/// from HTTP request bodies. This parser handles file uploads and is directly
/// exposed to untrusted network data.
/// </summary>
internal sealed class MultipartReaderFuzzer : IFuzzer
{
    public string[] TargetAssemblies => ["Microsoft.AspNetCore.WebUtilities"];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        // Use first two bytes to derive a boundary length (1-32 chars).
        int boundaryLength = (bytes[0] % 32) + 1;
        int headerOffset = 2;

        if (bytes.Length < headerOffset + boundaryLength)
        {
            return;
        }

        // Extract a boundary string from the input bytes.
        var boundary = Encoding.ASCII.GetString(bytes.Slice(headerOffset, boundaryLength));

        // Sanitize boundary: replace any null chars to avoid ArgumentException.
        boundary = boundary.Replace('\0', 'x');
        if (string.IsNullOrWhiteSpace(boundary))
        {
            boundary = "boundary";
        }

        var body = bytes[(headerOffset + boundaryLength)..];

        TestMultipartReader(body.ToArray(), boundary, async: false).GetAwaiter().GetResult();
        TestMultipartReader(body.ToArray(), boundary, async: true).GetAwaiter().GetResult();
    }

    private static async Task TestMultipartReader(byte[] body, string boundary, bool async)
    {
        using var stream = new MemoryStream(body);

        try
        {
            var reader = new MultipartReader(boundary, stream);
            reader.HeadersCountLimit = 32;
            reader.HeadersLengthLimit = 1024 * 8;
            reader.BodyLengthLimit = 1024 * 64;

            // Read all sections until null (end of multipart) or limit.
            var section = await reader.ReadNextSectionAsync(CancellationToken.None);
            while (section != null)
            {
                // Access properties to simulate usage
                _ = section.ContentType;
                _ = section.ContentDisposition;
                Assert.NotNull(section.Body);

                // Drain the section body to exercise the stream reading logic.
                var buffer = new byte[1024];
                int totalRead = 0;
                int read;
                if (async)
                {
                    read = await section.Body.ReadAsync(buffer);
                }
                else
                {
                    read = section.Body.Read(buffer, 0, buffer.Length);
                }

                while (read > 0 && totalRead < 64 * 1024)
                {
                    totalRead += read;
                }

                section = await reader.ReadNextSectionAsync(CancellationToken.None);
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or InvalidOperationException or ArgumentException)
        {
            // Expected for malformed multipart data.
        }
    }
}
