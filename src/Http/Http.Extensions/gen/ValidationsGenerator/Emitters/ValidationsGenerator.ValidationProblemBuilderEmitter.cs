// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator
{
    internal static string EmitValidationProblemBuilder()
    {
        var writer = new StringWriter();
        var code = new CodeWriter(writer, baseIndent: 1);
        code.WriteLine("file class ValidationProblemBuilder");
        code.StartBlock();
        code.WriteLine("private readonly global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails _problemDetails;");
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder()");
        code.StartBlock();
        code.WriteLine("_problemDetails = new global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails();");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithTitle(string title)");
        code.StartBlock();
        code.WriteLine("_problemDetails.Title = title;");
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithStatus(int? status)");
        code.StartBlock();
        code.WriteLine("_problemDetails.Status = status;");
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithDetail(string detail)");
        code.StartBlock();
        code.WriteLine("_problemDetails.Detail = detail;");
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithInstance(string instance)");
        code.StartBlock();
        code.WriteLine("_problemDetails.Instance = instance;");
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithType(string type)");
        code.StartBlock();
        code.WriteLine("_problemDetails.Type = type;");
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithExtensions(global::System.Collections.Generic.IDictionary<string, object> extensions)");
        code.StartBlock();
        code.WriteLine("foreach (var kvp in extensions)");
        code.StartBlock();
        code.WriteLine("_problemDetails.Extensions[kvp.Key] = kvp.Value;");
        code.EndBlock();
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithErrors(global::System.Collections.Generic.IDictionary<string, string[]> errors)");
        code.StartBlock();
        code.WriteLine("foreach (var kvp in errors)");
        code.StartBlock();
        code.WriteLine("_problemDetails.Errors[kvp.Key] = kvp.Value;");
        code.EndBlock();
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithError(string key, string error)");
        code.StartBlock();
        code.WriteLine("if (_problemDetails.Errors.ContainsKey(key))");
        code.StartBlock();
        code.WriteLine("_problemDetails.Errors[key] = _problemDetails.Errors[key].Append(error).ToArray();");
        code.EndBlock();
        code.WriteLine("else");
        code.StartBlock();
        code.WriteLine("_problemDetails.Errors[key] = new string[] { error };");
        code.EndBlock();
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public ValidationProblemBuilder WithErrors(string key, string[] errors)");
        code.StartBlock();
        code.WriteLine("if (_problemDetails.Errors.ContainsKey(key))");
        code.StartBlock();
        code.WriteLine("_problemDetails.Errors[key] = _problemDetails.Errors[key].Concat(errors).ToArray();");
        code.EndBlock();
        code.WriteLine("else");
        code.StartBlock();
        code.WriteLine("_problemDetails.Errors[key] = errors;");
        code.EndBlock();
        code.WriteLine("return this;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public global::Microsoft.AspNetCore.Http.HttpValidationProblemDetails Build()");
        code.StartBlock();
        code.WriteLine("return _problemDetails;");
        code.EndBlock();
        code.WriteLine();
        code.WriteLine("public bool HasValue()");
        code.StartBlock();
        code.WriteLine("return _problemDetails.Errors.Count > 0;");
        code.EndBlock();
        code.EndBlock();

        return writer.ToString();
    }
}
