// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

public class FormReaderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_EmptyKeyAtEndAllowed(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "=bar");

        var formCollection = await ReadFormAsync(new FormReader(body));

        Assert.Equal("bar", formCollection[""].ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_EmptyKeyWithAdditionalEntryAllowed(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "=bar&baz=2");

        var formCollection = await ReadFormAsync(new FormReader(body));

        Assert.Equal("bar", formCollection[""].ToString());
        Assert.Equal("2", formCollection["baz"].ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_EmptyValuedAtEndAllowed(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=");

        var formCollection = await ReadFormAsync(new FormReader(body));

        Assert.Equal("", formCollection["foo"].ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_EmptyValuedWithAdditionalEntryAllowed(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=&baz=2");

        var formCollection = await ReadFormAsync(new FormReader(body));

        Assert.Equal("", formCollection["foo"].ToString());
        Assert.Equal("2", formCollection["baz"].ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueCountLimitMet_Success(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=1&bar=2&baz=3");

        var formCollection = await ReadFormAsync(new FormReader(body) { ValueCountLimit = 3 });

        Assert.Equal("1", formCollection["foo"].ToString());
        Assert.Equal("2", formCollection["bar"].ToString());
        Assert.Equal("3", formCollection["baz"].ToString());
        Assert.Equal(3, formCollection.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueCountLimitExceeded_Throw(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=1&baz=2&bar=3&baz=4&baf=5");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => ReadFormAsync(new FormReader(body) { ValueCountLimit = 3 }));
        Assert.Equal("Form value count limit 3 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueCountLimitExceededSameKey_Throw(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "baz=1&baz=2&baz=3&baz=4");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => ReadFormAsync(new FormReader(body) { ValueCountLimit = 3 }));
        Assert.Equal("Form value count limit 3 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_KeyLengthLimitMet_Success(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=1&bar=2&baz=3&baz=4");

        var formCollection = await ReadFormAsync(new FormReader(body) { KeyLengthLimit = 10 });

        Assert.Equal("1", formCollection["foo"].ToString());
        Assert.Equal("2", formCollection["bar"].ToString());
        Assert.Equal("3,4", formCollection["baz"].ToString());
        Assert.Equal(3, formCollection.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_KeyLengthLimitExceeded_Throw(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=1&baz1234567890=2");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => ReadFormAsync(new FormReader(body) { KeyLengthLimit = 10 }));
        Assert.Equal("Form key or value length limit 10 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueLengthLimitMet_Success(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=1&bar=1234567890&baz=3&baz=4");

        var formCollection = await ReadFormAsync(new FormReader(body) { ValueLengthLimit = 10 });

        Assert.Equal("1", formCollection["foo"].ToString());
        Assert.Equal("1234567890", formCollection["bar"].ToString());
        Assert.Equal("3,4", formCollection["baz"].ToString());
        Assert.Equal(3, formCollection.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueLengthLimitExceeded_Throw(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=1&baz=1234567890123");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => ReadFormAsync(new FormReader(body) { ValueLengthLimit = 10 }));
        Assert.Equal("Form key or value length limit 10 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadNextPair_ReadsAllPairs(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "foo=&baz=2");

        var reader = new FormReader(body);

        var pair = (KeyValuePair<string, string>)await ReadPair(reader);

        Assert.Equal("foo", pair.Key);
        Assert.Equal("", pair.Value);

        pair = (KeyValuePair<string, string>)await ReadPair(reader);

        Assert.Equal("baz", pair.Key);
        Assert.Equal("2", pair.Value);

        Assert.Null(await ReadPair(reader));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadNextPair_ReturnsNullOnEmptyStream(bool bufferRequest)
    {
        var body = MakeStream(bufferRequest, "");

        var reader = new FormReader(body);

        Assert.Null(await ReadPair(reader));
    }

    // https://en.wikipedia.org/wiki/Percent-encoding
    [Theory]
    [InlineData("++=hello", "  ", "hello")]
    [InlineData("a=1+1", "a", "1 1")]
    [InlineData("%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E=%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E", "\"%-.<>\\^_`{|}~", "\"%-.<>\\^_`{|}~")]
    [InlineData("a=%41", "a", "A")] // ascii encoded hex
    [InlineData("a=%C3%A1", "a", "\u00e1")] // utf8 code points
    [InlineData("a=%u20AC", "a", "%u20AC")] // utf16 not supported
    public async Task ReadForm_Decoding(string formData, string key, string expectedValue)
    {
        var body = MakeStream(bufferRequest: false, text: formData);

        var form = await ReadFormAsync(new FormReader(body));

        Assert.Equal(expectedValue, form[key]);
    }

    protected virtual Task<Dictionary<string, StringValues>> ReadFormAsync(FormReader reader)
    {
        return Task.FromResult(reader.ReadForm());
    }

    protected virtual Task<KeyValuePair<string, string>?> ReadPair(FormReader reader)
    {
        return Task.FromResult(reader.ReadNextPair());
    }

    private static Stream MakeStream(bool bufferRequest, string text)
    {
        var formContent = Encoding.UTF8.GetBytes(text);
        Stream body = new MemoryStream(formContent);
        if (!bufferRequest)
        {
            body = new NonSeekableReadStream(body);
        }
        return body;
    }
}
