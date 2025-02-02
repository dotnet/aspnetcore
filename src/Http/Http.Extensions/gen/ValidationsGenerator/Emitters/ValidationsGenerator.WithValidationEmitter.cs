// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator
{
#pragma warning disable RSEXPERIMENTAL002
    internal static string EmitWithValidationInterception(InterceptableLocation? location, CancellationToken cancellationToken)
    {
        AnalyzerDebug.Assert(location != null, "Interceptable location should not be null.");
        var writer = new StringWriter();
        var code = new CodeWriter(writer, baseIndent: 1);
        code.WriteLine("file static class WithValidationsInterceptor");
        code.StartBlock();
        code.WriteLine(location.GetInterceptsLocationAttributeSyntax());
        code.WriteLine("public static global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder WithValidation(this global::Microsoft.AspNetCore.Builder.IEndpointConventionBuilder builder)");
        code.StartBlock();
        code.WriteLine("System.Diagnostics.Debugger.Break();");
        code.WriteLine("builder.AddEndpointFilter(async (context, next) =>");
        code.StartBlock();
        code.WriteLine("var targetEndpoint = context.HttpContext.Features.Get<global::Microsoft.AspNetCore.Http.Features.IEndpointFeature>()?.Endpoint;");
        code.WriteLine("Debug.Assert(targetEndpoint != null);");
        code.WriteLine("var route = ((global::Microsoft.AspNetCore.Routing.RouteEndpoint)targetEndpoint).RoutePattern.RawText;");
        code.WriteLine(@"var methods = ((global::Microsoft.AspNetCore.Routing.RouteEndpoint)targetEndpoint).Metadata.GetMetadata<global::Microsoft.AspNetCore.Routing.IHttpMethodMetadata>()?.HttpMethods ?? [""GET""];");
        code.WriteLine("Debug.Assert(route != null);");
        code.WriteLine("var validationFilter = ValidationsFilters.Filters[new EndpointKey(route, methods)];");
        code.WriteLine("var validationProblemDetails = validationFilter(context);");
        code.WriteLine("if (validationProblemDetails == null)");
        code.StartBlock();
        code.WriteLine("return await next(context);");
        code.EndBlock();
        code.WriteLine("return global::Microsoft.AspNetCore.Http.TypedResults.ValidationProblem(validationProblemDetails.Errors);");
        code.Indent--;
        code.WriteLine("});");
        code.WriteLine("return builder;");
        code.EndBlock();
        code.EndBlock();
        return writer.ToString();
    }
#pragma warning restore RSEXPERIMENTAL002
}
