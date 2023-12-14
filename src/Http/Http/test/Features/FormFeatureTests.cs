// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Text;

namespace Microsoft.AspNetCore.Http.Features;

public class FormFeatureTests
{
    [Fact]
    public async Task ReadFormAsync_0ContentLength_ReturnsEmptyForm()
    {
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.ContentLength = 0;

        var formFeature = new FormFeature(context.Request, new FormOptions());
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = await context.Request.ReadFormAsync();

        Assert.Same(FormCollection.Empty, formCollection);
    }

    [Fact]
    public async Task FormFeatureReadsOptionsFromDefaultHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
        context.FormOptions = new FormOptions
        {
            ValueCountLimit = 1
        };

        var formContent = Encoding.UTF8.GetBytes("foo=bar&baz=2");
        context.Request.Body = new NonSeekableReadStream(formContent);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => context.Request.ReadFormAsync());

        Assert.Equal("Form value count limit 1 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_SimpleData_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes("foo=bar&baz=2");
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = await context.Request.ReadFormAsync();

        Assert.Equal("bar", formCollection["foo"]);
        Assert.Equal("2", formCollection["baz"]);
        Assert.Equal(bufferRequest, context.Request.Body.CanSeek);
        if (bufferRequest)
        {
            Assert.Equal(0, context.Request.Body.Position);
        }

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formFeature.Form, formCollection);

        // Cleanup
        await responseFeature.CompleteAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_SimpleData_ReplacePipeReader_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes("foo=bar&baz=2");
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";

        var pipe = new Pipe();
        await pipe.Writer.WriteAsync(formContent);
        pipe.Writer.Complete();

        var mockFeature = new MockRequestBodyPipeFeature();
        mockFeature.Reader = pipe.Reader;
        context.Features.Set<IRequestBodyPipeFeature>(mockFeature);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = await context.Request.ReadFormAsync();

        Assert.Equal("bar", formCollection["foo"]);
        Assert.Equal("2", formCollection["baz"]);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formFeature.Form, formCollection);

        // Cleanup
        await responseFeature.CompleteAsync();
    }

    private class MockRequestBodyPipeFeature : IRequestBodyPipeFeature
    {
        public PipeReader Reader { get; set; }
    }

    private const string MultipartContentType = "multipart/form-data; boundary=WebKitFormBoundary5pDRpGheQXaM8k3T";

    private const string MultipartContentTypeWithSpecialCharacters = "multipart/form-data; boundary=\"WebKitFormBoundary/:5pDRpGheQXaM8k3T\"";

    private const string EmptyMultipartForm = "--WebKitFormBoundary5pDRpGheQXaM8k3T--";

    // Note that CRLF (\r\n) is required. You can't use multi-line C# strings here because the line breaks on Linux are just LF.
    private const string MultipartFormEnd = "--WebKitFormBoundary5pDRpGheQXaM8k3T--\r\n";

    private const string MultipartFormEndWithSpecialCharacters = "--WebKitFormBoundary/:5pDRpGheQXaM8k3T--\r\n";

    private const string MultipartFormField = "--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"description\"\r\n" +
"\r\n" +
"Foo\r\n";

    private const string MultipartFormFile = "--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"myfile1\"; filename=\"temp.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n" +
"<html><body>Hello World</body></html>\r\n";

    private const string MultipartFormEncodedFilename = "--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"myfile1\"; filename=\"temp.html\"; filename*=utf-8\'\'t%c3%a9mp.html\r\n" +
"Content-Type: text/html\r\n" +
"\r\n" +
"<html><body>Hello World</body></html>\r\n";

    private const string MultipartFormFileSpecialCharacters = "--WebKitFormBoundary/:5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"description\"\r\n" +
"\r\n" +
"Foo\r\n";

    private const string InvalidContentDispositionValue = "form-data; name=\"description\" - filename=\"temp.html\"";

    private const string MultipartFormFileInvalidContentDispositionValue = "--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: " +
InvalidContentDispositionValue +
"\r\n" +
"\r\n" +
"Foo\r\n";

    private const string MultipartFormFileNonFormOrFileContentDispositionValue = "--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition:x" +
"\r\n" +
"\r\n" +
"Foo\r\n";

    private const string MultipartFormWithField =
        MultipartFormField +
        MultipartFormEnd;

    private const string MultipartFormWithFile =
        MultipartFormFile +
        MultipartFormEnd;

    private const string MultipartFormWithFieldAndFile =
        MultipartFormField +
        MultipartFormFile +
        MultipartFormEnd;

    private const string MultipartFormWithEncodedFilename =
        MultipartFormEncodedFilename +
        MultipartFormEnd;

    private const string MultipartFormWithSpecialCharacters =
        MultipartFormFileSpecialCharacters +
        MultipartFormEndWithSpecialCharacters;

    private const string MultipartFormWithInvalidContentDispositionValue =
        MultipartFormFileInvalidContentDispositionValue +
        MultipartFormEnd;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadForm_EmptyMultipart_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes(EmptyMultipartForm);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = context.Request.Form;

        Assert.NotNull(formCollection);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formCollection, formFeature.Form);
        Assert.Same(formCollection, await context.Request.ReadFormAsync());

        // Content
        Assert.Equal(0, formCollection.Count);
        Assert.NotNull(formCollection.Files);
        Assert.Equal(0, formCollection.Files.Count);

        // Cleanup
        await responseFeature.CompleteAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadForm_MultipartWithField_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes(MultipartFormWithField);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = context.Request.Form;

        Assert.NotNull(formCollection);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formCollection, formFeature.Form);
        Assert.Same(formCollection, await context.Request.ReadFormAsync());

        // Content
        Assert.Equal(1, formCollection.Count);
        Assert.Equal("Foo", formCollection["description"]);

        Assert.NotNull(formCollection.Files);
        Assert.Equal(0, formCollection.Files.Count);

        // Cleanup
        await responseFeature.CompleteAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_MultipartWithFile_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes(MultipartFormWithFile);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = await context.Request.ReadFormAsync();

        Assert.NotNull(formCollection);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formFeature.Form, formCollection);
        Assert.Same(formCollection, context.Request.Form);

        // Content
        Assert.Equal(0, formCollection.Count);

        Assert.NotNull(formCollection.Files);
        Assert.Equal(1, formCollection.Files.Count);

        var file = formCollection.Files["myfile1"];
        Assert.Equal("myfile1", file.Name);
        Assert.Equal("temp.html", file.FileName);
        Assert.Equal("text/html", file.ContentType);
        Assert.Equal(@"form-data; name=""myfile1""; filename=""temp.html""", file.ContentDisposition);
        var body = file.OpenReadStream();
        using (var reader = new StreamReader(body))
        {
            Assert.True(body.CanSeek);
            var content = reader.ReadToEnd();
            Assert.Equal("<html><body>Hello World</body></html>", content);
        }

        await responseFeature.CompleteAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_MultipartWithFileAndQuotedBoundaryString_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes(MultipartFormWithSpecialCharacters);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentTypeWithSpecialCharacters;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = context.Request.Form;

        Assert.NotNull(formCollection);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formCollection, formFeature.Form);
        Assert.Same(formCollection, await context.Request.ReadFormAsync());

        // Content
        Assert.Equal(1, formCollection.Count);
        Assert.Equal("Foo", formCollection["description"]);

        Assert.NotNull(formCollection.Files);
        Assert.Equal(0, formCollection.Files.Count);

        // Cleanup
        await responseFeature.CompleteAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_MultipartWithEncodedFilename_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes(MultipartFormWithEncodedFilename);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = await context.Request.ReadFormAsync();

        Assert.NotNull(formCollection);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formFeature.Form, formCollection);
        Assert.Same(formCollection, context.Request.Form);

        // Content
        Assert.Equal(0, formCollection.Count);

        Assert.NotNull(formCollection.Files);
        Assert.Equal(1, formCollection.Files.Count);

        var file = formCollection.Files["myfile1"];
        Assert.Equal("myfile1", file.Name);
        Assert.Equal("t\u00e9mp.html", file.FileName);
        Assert.Equal("text/html", file.ContentType);
        Assert.Equal(@"form-data; name=""myfile1""; filename=""temp.html""; filename*=utf-8''t%c3%a9mp.html", file.ContentDisposition);
        var body = file.OpenReadStream();
        using (var reader = new StreamReader(body))
        {
            Assert.True(body.CanSeek);
            var content = reader.ReadToEnd();
            Assert.Equal("<html><body>Hello World</body></html>", content);
        }

        await responseFeature.CompleteAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_MultipartWithFieldAndFile_ReturnsParsedFormCollection(bool bufferRequest)
    {
        var formContent = Encoding.UTF8.GetBytes(MultipartFormWithFieldAndFile);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = await context.Request.ReadFormAsync();

        Assert.NotNull(formCollection);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formFeature.Form, formCollection);
        Assert.Same(formCollection, context.Request.Form);

        // Content
        Assert.Equal(1, formCollection.Count);
        Assert.Equal("Foo", formCollection["description"]);

        Assert.NotNull(formCollection.Files);
        Assert.Equal(1, formCollection.Files.Count);

        var file = formCollection.Files["myfile1"];
        Assert.Equal("text/html", file.ContentType);
        Assert.Equal(@"form-data; name=""myfile1""; filename=""temp.html""", file.ContentDisposition);
        var body = file.OpenReadStream();
        using (var reader = new StreamReader(body))
        {
            Assert.True(body.CanSeek);
            var content = reader.ReadToEnd();
            Assert.Equal("<html><body>Hello World</body></html>", content);
        }

        await responseFeature.CompleteAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_NonFormOrFieldContentDisposition_ValueCountLimitExceeded_Throw(bool bufferRequest)
    {
        var formContent = new List<byte>();
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFileNonFormOrFileContentDispositionValue));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFileNonFormOrFileContentDispositionValue));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFileNonFormOrFileContentDispositionValue));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormEnd));

        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent.ToArray());

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest, ValueCountLimit = 2 });
        context.Features.Set<IFormFeature>(formFeature);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => context.Request.ReadFormAsync());
        Assert.Equal("Form value count limit 2 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueCountLimitExceeded_Throw(bool bufferRequest)
    {
        var formContent = new List<byte>();
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormField));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormField));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormField));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormEnd));

        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent.ToArray());

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest, ValueCountLimit = 2 });
        context.Features.Set<IFormFeature>(formFeature);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => context.Request.ReadFormAsync());
        Assert.Equal("Form value count limit 2 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueCountLimitExceededWithFiles_Throw(bool bufferRequest)
    {
        var formContent = new List<byte>();
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFile));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFile));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFile));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormEnd));

        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent.ToArray());

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest, ValueCountLimit = 2 });
        context.Features.Set<IFormFeature>(formFeature);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => context.Request.ReadFormAsync());
        Assert.Equal("Form value count limit 2 exceeded.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadFormAsync_ValueCountLimitExceededWithMixedDisposition_Throw(bool bufferRequest)
    {
        var formContent = new List<byte>();
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormField));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFile));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormFileNonFormOrFileContentDispositionValue));
        formContent.AddRange(Encoding.UTF8.GetBytes(MultipartFormEnd));

        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent.ToArray());

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest, ValueCountLimit = 2 });
        context.Features.Set<IFormFeature>(formFeature);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => context.Request.ReadFormAsync());
        Assert.Equal("Form value count limit 2 exceeded.", exception.Message);
    }

    [Theory]
    // FileBufferingReadStream transitions to disk storage after 30kb, and stops pooling buffers at 1mb.
    [InlineData(true, 1024)]
    [InlineData(false, 1024)]
    [InlineData(true, 40 * 1024)]
    [InlineData(false, 40 * 1024)]
    [InlineData(true, 4 * 1024 * 1024)]
    [InlineData(false, 4 * 1024 * 1024)]
    public async Task ReadFormAsync_MultipartWithFieldAndMediumFile_ReturnsParsedFormCollection(bool bufferRequest, int fileSize)
    {
        var fileContents = CreateFile(fileSize);
        var formContent = CreateMultipartWithFormAndFile(fileContents);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions() { BufferBody = bufferRequest });
        context.Features.Set<IFormFeature>(formFeature);

        var formCollection = await context.Request.ReadFormAsync();

        Assert.NotNull(formCollection);

        // Cached
        formFeature = context.Features.Get<IFormFeature>();
        Assert.NotNull(formFeature);
        Assert.NotNull(formFeature.Form);
        Assert.Same(formFeature.Form, formCollection);
        Assert.Same(formCollection, context.Request.Form);

        // Content
        Assert.Equal(1, formCollection.Count);
        Assert.Equal("Foo", formCollection["description"]);

        Assert.NotNull(formCollection.Files);
        Assert.Equal(1, formCollection.Files.Count);

        var file = formCollection.Files["myfile1"];
        Assert.Equal("text/html", file.ContentType);
        Assert.Equal(@"form-data; name=""myfile1""; filename=""temp.html""", file.ContentDisposition);
        using (var body = file.OpenReadStream())
        {
            Assert.True(body.CanSeek);
            CompareStreams(fileContents, body);
        }

        await responseFeature.CompleteAsync();
    }

    [Fact]
    public async Task ReadFormAsync_MultipartWithInvalidContentDisposition_Throw()
    {
        var formContent = Encoding.UTF8.GetBytes(MultipartFormWithInvalidContentDispositionValue);
        var context = new DefaultHttpContext();
        var responseFeature = new FakeResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Request.ContentType = MultipartContentType;
        context.Request.Body = new NonSeekableReadStream(formContent);

        IFormFeature formFeature = new FormFeature(context.Request, new FormOptions());
        context.Features.Set<IFormFeature>(formFeature);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => context.Request.ReadFormAsync());

        Assert.Equal("Form section has invalid Content-Disposition value: " + InvalidContentDispositionValue, exception.Message);
    }

    private Stream CreateFile(int size)
    {
        var stream = new MemoryStream(size);
        var bytes = Encoding.ASCII.GetBytes("HelloWorld_ABCDEFGHIJKLMNOPQRSTUVWXYZ.abcdefghijklmnopqrstuvwxyz,0123456789;");
        int written = 0;
        while (written < size)
        {
            var toWrite = Math.Min(size - written, bytes.Length);
            stream.Write(bytes, 0, toWrite);
            written += toWrite;
        }
        stream.Position = 0;
        return stream;
    }

    private Stream CreateMultipartWithFormAndFile(Stream fileContents)
    {
        var stream = new MemoryStream();
        var header =
MultipartFormField +
"--WebKitFormBoundary5pDRpGheQXaM8k3T\r\n" +
"Content-Disposition: form-data; name=\"myfile1\"; filename=\"temp.html\"\r\n" +
"Content-Type: text/html\r\n" +
"\r\n";
        var footer =
"\r\n--WebKitFormBoundary5pDRpGheQXaM8k3T--";

        var bytes = Encoding.ASCII.GetBytes(header);
        stream.Write(bytes, 0, bytes.Length);

        fileContents.CopyTo(stream);
        fileContents.Position = 0;

        bytes = Encoding.ASCII.GetBytes(footer);
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;
        return stream;
    }

    private void CompareStreams(Stream streamA, Stream streamB)
    {
        Assert.Equal(streamA.Length, streamB.Length);
        byte[] bytesA = new byte[1024], bytesB = new byte[1024];
        var readA = streamA.Read(bytesA, 0, bytesA.Length);
        var readB = streamB.Read(bytesB, 0, bytesB.Length);
        Assert.Equal(readA, readB);
        var loops = 0;
        while (readA > 0)
        {
            for (int i = 0; i < readA; i++)
            {
                if (bytesA[i] != bytesB[i])
                {
                    throw new Exception($"Value mismatch at loop {loops}, index {i}; A:{bytesA[i]}, B:{bytesB[i]}");
                }
            }

            readA = streamA.Read(bytesA, 0, bytesA.Length);
            readB = streamB.Read(bytesB, 0, bytesB.Length);
            Assert.Equal(readA, readB);
            loops++;
        }
    }
}
