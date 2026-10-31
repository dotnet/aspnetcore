// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class ObjectResultExecutorBenchmark
{
    private ObjectResultExecutor _executor;
    private ActionContext _actionContext;
    private DefaultHttpContext _httpContext;
    private ObjectResult _result;

    [Params("", "application/json", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    public string Accept { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var options = new MvcOptions();
        options.OutputFormatters.Add(new HttpNoContentOutputFormatter());
        options.OutputFormatters.Add(new StringOutputFormatter());
        options.OutputFormatters.Add(new StreamOutputFormatter());
        options.OutputFormatters.Add(new SystemTextJsonOutputFormatter(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() }));

        var services = new ServiceCollection().BuildServiceProvider();
        var selector = new DefaultOutputFormatterSelector(Options.Create(options), NullLoggerFactory.Instance);
        _executor = new ObjectResultExecutor(
            selector,
            new TestHttpResponseStreamWriterFactory(),
            NullLoggerFactory.Instance,
            Options.Create(options));

        _httpContext = new DefaultHttpContext { RequestServices = services };
        _actionContext = new ActionContext(_httpContext, new RouteData(), new ActionDescriptor());
        _result = new ObjectResult(new Model { Id = 1, Name = "Hello" });
    }

    [Benchmark]
    public Task Execute()
    {
        if (Accept.Length == 0)
        {
            _httpContext.Request.Headers.Remove("Accept");
        }
        else
        {
            _httpContext.Request.Headers.Accept = Accept;
        }
        _httpContext.Response.Headers.Clear();
        _httpContext.Response.Body = Stream.Null;
        return _executor.ExecuteAsync(_actionContext, _result);
    }

    public class Model
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private sealed class TestHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
    {
        public TextWriter CreateWriter(Stream stream, System.Text.Encoding encoding) => new StreamWriter(stream, encoding);
    }
}
