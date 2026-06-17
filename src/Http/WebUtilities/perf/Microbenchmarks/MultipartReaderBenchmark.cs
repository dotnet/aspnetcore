// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.AspNetCore.WebUtilities.Microbenchmarks;

public class MultipartReaderBenchmark
{
    MemoryStream _stream;

    [Params(6, 28, 70)]
    public int BoundarySize { get; set; }

    [Params(1, 2, 3)]
    public int SectionCount { get; set; }

    [Params(true, false)]
    public bool LargePayload { get; set; }

    private string Boundary { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string data;
        switch (BoundarySize)
        {
            case 6:
                Boundary = "abc123";
                break;
            case 28:
                Boundary = "9051914041544843365972754266";
                break;
            case 70:
                Boundary = "WbQvpJcaxJRjMqwLdioBSOyJk3fHYdo9hLCOSBkHYW70pU71tH3lJq6ZUcSErOl0NRn5uT";
                break;
            default:
                throw new InvalidOperationException(nameof(BoundarySize));
        }

        switch (SectionCount)
        {
            case 1:
                data = $"--{Boundary}\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
$"text default{new string('a', LargePayload ? 10000000 : 0)}\r\n" +
$"--{Boundary}--\r\n"; ;
                break;
            case 2:
                data = $"--{Boundary}\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
$"--{Boundary}\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
$"Content of a.txt.{new string('a', LargePayload ? 10000000 : 0)}\r\n" +
"\r\n" +
$"--{Boundary}--\r\n";
                break;
            case 3:
                data = $"--{Boundary}\r\n" +
"Content-Disposition: form-data; name=\"text\"\r\n" +
"\r\n" +
"text default\r\n" +
$"--{Boundary}\r\n" +
"Content-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\n" +
"Content-Type: text/plain\r\n" +
"\r\n" +
$"Content of a.txt{new string('a', LargePayload ? 10000000 : 0)}\r\n" +
"\r\n" +
$"--{Boundary}\r\n" +
"Content-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n" +
"<!DOCTYPE html><title>Content of a.html.</title>\r\n" +
"\r\n" +
$"--{Boundary}--\r\n";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(SectionCount));
        }
        _stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
    }

    [Benchmark]
    public async Task MultipartReaderParsing()
    {
        var reader = new MultipartReader(Boundary, _stream);

        var sectionCount = 0;
        MultipartSection section;
        do
        {
            section = await reader.ReadNextSectionAsync();
            sectionCount += section is not null ? 1 : 0;
        }
        while (section is not null);

        _stream.Position = 0;

        if (sectionCount != SectionCount)
        {
            throw new InvalidOperationException();
        }
    }

    [Benchmark]
    public async Task MultipartReaderParsingWithRead()
    {
        var reader = new MultipartReader(Boundary, _stream);

        var sectionCount = 0;
        MultipartSection section;
        do
        {
            section = await reader.ReadNextSectionAsync();
            sectionCount += section is not null ? 1 : 0;

            section?.Body.CopyTo(NullStream.Instance);
        }
        while (section is not null);

        _stream.Position = 0;

        if (sectionCount != SectionCount)
        {
            throw new InvalidOperationException();
        }
    }

    public class NullStream : Stream
    {
        public static readonly NullStream Instance = new NullStream();

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => 0;

        public override long Position { get => 0; set { } }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => 0;

        public override long Seek(long offset, SeekOrigin origin) => 0;

        public override void SetLength(long value) { }

        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
