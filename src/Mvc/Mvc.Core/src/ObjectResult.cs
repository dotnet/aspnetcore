// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that on execution will write an object to the response
/// using mechanisms provided by the host.
/// </summary>
public class ObjectResult : ActionResult, IStatusCodeActionResult
{
    private MediaTypeCollection _contentTypes;

    /// <summary>
    /// Creates a new <see cref="ObjectResult"/> instance with the provided <paramref name="value"/>.
    /// </summary>
    /// <param name="value"></param>
    public ObjectResult(object? value)
    {
        Value = value;
        Formatters = new FormatterCollection<IOutputFormatter>();
        _contentTypes = new MediaTypeCollection();
    }

    /// <summary>
    /// The object result.
    /// </summary>
    [ActionResultObjectValue]
    public object? Value { get; set; }

    /// <summary>
    /// The collection of <see cref="IOutputFormatter"/>.
    /// </summary>
    public FormatterCollection<IOutputFormatter> Formatters { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="MediaTypeCollection"/>.
    /// </summary>
    public MediaTypeCollection ContentTypes
    {
        get => _contentTypes;
        set => _contentTypes = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the declared type.
    /// </summary>
    public Type? DeclaredType { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <inheritdoc/>
    public override Task ExecuteResultAsync(ActionContext context)
    {
        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ObjectResult>>();
        return executor.ExecuteAsync(context, this);
    }

    /// <summary>
    /// This method is called before the formatter writes to the output stream.
    /// </summary>
    public virtual void OnFormatting(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Value is ProblemDetails details)
        {
            if (details.Status != null && StatusCode == null)
            {
                StatusCode = details.Status;
            }
            else if (details.Status == null && StatusCode != null)
            {
                details.Status = StatusCode;
            }
        }

        if (StatusCode.HasValue)
        {
            context.HttpContext.Response.StatusCode = StatusCode.Value;
        }
    }
}
