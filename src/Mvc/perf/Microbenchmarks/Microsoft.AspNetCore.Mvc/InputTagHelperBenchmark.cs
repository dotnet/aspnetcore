// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class InputTagHelperBenchmark
{
    private static readonly Func<bool, HtmlEncoder, Task<TagHelperContent>> _getChildContent =
        (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());

    private IHtmlGenerator _generator;
    private ViewContext _viewContext;
    private ModelExpression _for;
    private TagHelperContext _context;

    [Params(nameof(Model.Name), nameof(Model.Email), nameof(Model.Age), nameof(Model.IsActive))]
    public string Property { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var diagnosticListener = new DiagnosticListener("Microsoft.AspNetCore");
        services.AddSingleton(diagnosticListener);
        services.AddSingleton<DiagnosticSource>(diagnosticListener);
        services.AddControllersWithViews();
        var serviceProvider = services.BuildServiceProvider();

        _generator = serviceProvider.GetRequiredService<IHtmlGenerator>();
        var metadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
        var expressionProvider = (ModelExpressionProvider)serviceProvider.GetRequiredService<IModelExpressionProvider>();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary<Model>(metadataProvider, new ModelStateDictionary())
        {
            Model = new Model { Name = "Name", Email = "a@b.com", Age = 42, IsActive = true },
        };

        _viewContext = new ViewContext(
            actionContext,
            NullView.Instance,
            viewData,
            new TempDataDictionary(httpContext, new NullTempDataProvider()),
            TextWriter.Null,
            new HtmlHelperOptions());

        _for = expressionProvider.CreateModelExpression(viewData, Property);
        _context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "id");
    }

    [Benchmark]
    public TagHelperOutput Process()
    {
        // Client validation attributes are only rendered once per field in a form.
        _viewContext.FormContext = new FormContext();

        var tagHelper = new InputTagHelper(_generator)
        {
            ViewContext = _viewContext,
            For = _for,
        };

        var output = new TagHelperOutput("input", new TagHelperAttributeList(), _getChildContent);
        tagHelper.Process(_context, output);
        return output;
    }

    public class Model
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Range(0, 150)]
        public int Age { get; set; }

        public bool IsActive { get; set; }
    }

    private sealed class NullView : IView
    {
        public static readonly NullView Instance = new();

        public string Path => string.Empty;

        public Task RenderAsync(ViewContext context) => Task.CompletedTask;
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
