// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.HPack;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class HPackDecoderBenchmark
{
    // Indexed Header Field Representation - Dynamic Table - Index 62 (first index in dynamic table)
    private static readonly byte[] _indexedHeaderDynamic = new byte[] { 0xbe };

    private static readonly byte[] _literalHeaderFieldWithoutIndexingNewName = new byte[] { 0x00 };

    private const string _headerNameString = "new-header";

    private static readonly byte[] _headerNameBytes = Encoding.ASCII.GetBytes(_headerNameString);

    private static readonly byte[] _headerName = new byte[] { (byte)_headerNameBytes.Length }
        .Concat(_headerNameBytes)
        .ToArray();

    private const string _headerValueString = "value";

    private static readonly byte[] _headerValueBytes = Encoding.ASCII.GetBytes(_headerValueString);

    private static readonly byte[] _headerValue = new byte[] { (byte)_headerValueBytes.Length }
        .Concat(_headerValueBytes)
        .ToArray();

    private static readonly byte[] _literalHeaderFieldNeverIndexed_NewName = _literalHeaderFieldWithoutIndexingNewName
            .Concat(_headerName)
            .Concat(_headerValue)
            .ToArray();

    private static readonly byte[] _literalHeaderFieldNeverIndexed_NewName_Large;
    private static readonly byte[] _literalHeaderFieldNeverIndexed_NewName_Multiple;
    private static readonly byte[] _indexedHeaderDynamic_Multiple;

    static HPackDecoderBenchmark()
    {
        string string8193 = new string('a', 8193);

        _literalHeaderFieldNeverIndexed_NewName_Large = _literalHeaderFieldWithoutIndexingNewName
            .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
            .Concat(Encoding.ASCII.GetBytes(string8193))
            .Concat(new byte[] { 0x7f, 0x82, 0x3f }) // 8193 encoded with 7-bit prefix, no Huffman encoding
            .Concat(Encoding.ASCII.GetBytes(string8193))
            .ToArray();

        _literalHeaderFieldNeverIndexed_NewName_Multiple = _literalHeaderFieldNeverIndexed_NewName
            .Concat(_literalHeaderFieldNeverIndexed_NewName)
            .Concat(_literalHeaderFieldNeverIndexed_NewName)
            .Concat(_literalHeaderFieldNeverIndexed_NewName)
            .Concat(_literalHeaderFieldNeverIndexed_NewName)
            .ToArray();

        _indexedHeaderDynamic_Multiple = _indexedHeaderDynamic
            .Concat(_indexedHeaderDynamic)
            .Concat(_indexedHeaderDynamic)
            .Concat(_indexedHeaderDynamic)
            .Concat(_indexedHeaderDynamic)
            .ToArray();
    }

    private HPackDecoder _decoder;
    private TestHeadersHandler _testHeadersHandler;
    private DynamicTable _dynamicTable;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _dynamicTable = new DynamicTable(maxSize: 4096);
        _dynamicTable.Insert(_headerNameBytes, _headerValueBytes);
        _decoder = new HPackDecoder(maxDynamicTableSize: 4096, maxHeadersLength: 65536, _dynamicTable);
        _testHeadersHandler = new TestHeadersHandler();
    }

    [Benchmark]
    public void DecodesLiteralHeaderFieldNeverIndexed_NewName()
    {
        _decoder.Decode(_literalHeaderFieldNeverIndexed_NewName, endHeaders: true, handler: _testHeadersHandler);
    }

    [Benchmark]
    public void DecodesLiteralHeaderFieldNeverIndexed_NewName_Large()
    {
        _decoder.Decode(_literalHeaderFieldNeverIndexed_NewName_Large, endHeaders: true, handler: _testHeadersHandler);
    }

    [Benchmark]
    public void DecodesLiteralHeaderFieldNeverIndexed_NewName_Multiple()
    {
        _decoder.Decode(_literalHeaderFieldNeverIndexed_NewName_Multiple, endHeaders: true, handler: _testHeadersHandler);
    }

    [Benchmark]
    public void DecodesIndexedHeaderField_DynamicTable()
    {
        _decoder.Decode(_indexedHeaderDynamic, endHeaders: true, handler: _testHeadersHandler);
    }

    [Benchmark]
    public void DecodesIndexedHeaderField_DynamicTable_Multiple()
    {
        _decoder.Decode(_indexedHeaderDynamic_Multiple, endHeaders: true, handler: _testHeadersHandler);
    }

    private sealed class TestHeadersHandler : IHttpStreamHeadersHandler
    {
        public void OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
        }

        public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
        }

        public void OnHeadersComplete(bool endStream)
        {
        }

        public void OnStaticIndexedHeader(int index)
        {
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
        }
    }
}
