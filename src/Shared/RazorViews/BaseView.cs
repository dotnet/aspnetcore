// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;

#nullable enable
#pragma warning disable IDE0060 // Unused members are required by codegen.

namespace Microsoft.Extensions.RazorViews;

/// <summary>
/// Infrastructure
/// </summary>
internal abstract class BaseView
{
    private static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly char[] NewLineChars = new[] { '\r', '\n' };
    private readonly Stack<TextWriter> _textWriterStack = new Stack<TextWriter>();

    /// <summary>
    /// The request context
    /// </summary>
    protected HttpContext Context { get; private set; } = default!;

    /// <summary>
    /// The request
    /// </summary>
    protected HttpRequest Request { get; private set; } = default!;

    /// <summary>
    /// The response
    /// </summary>
    protected HttpResponse Response { get; private set; } = default!;

    /// <summary>
    /// The output stream
    /// </summary>
    protected TextWriter Output { get; private set; } = default!;

    /// <summary>
    /// Html encoder used to encode content.
    /// </summary>
    protected HtmlEncoder HtmlEncoder { get; set; } = HtmlEncoder.Default;

    /// <summary>
    /// Url encoder used to encode content.
    /// </summary>
    protected UrlEncoder UrlEncoder { get; set; } = UrlEncoder.Default;

    /// <summary>
    /// JavaScript encoder used to encode content.
    /// </summary>
    protected JavaScriptEncoder JavaScriptEncoder { get; set; } = JavaScriptEncoder.Default;

    /// <summary>
    /// Execute an individual request
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    public async Task ExecuteAsync(Stream stream)
    {
        // We technically don't need this intermediate buffer if this method accepts a memory stream.
        var buffer = new MemoryStream();
        Output = new StreamWriter(buffer, UTF8NoBOM, 4096, leaveOpen: true);
        await ExecuteAsync();
        await Output.FlushAsync();
        await Output.DisposeAsync();
        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(stream);
    }

    /// <summary>
    /// Execute an individual request
    /// </summary>
    /// <param name="context"></param>
    public async Task ExecuteAsync(HttpContext context)
    {
        Context = context;
        Request = Context.Request;
        Response = Context.Response;
        var buffer = new MemoryStream();
        Output = new StreamWriter(buffer, UTF8NoBOM, 4096, leaveOpen: true);
        await ExecuteAsync();
        await Output.FlushAsync();
        await Output.DisposeAsync();
        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(Response.Body);
    }

    /// <summary>
    /// Execute an individual request
    /// </summary>
    public abstract Task ExecuteAsync();

    protected virtual void PushWriter(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        _textWriterStack.Push(Output);
        Output = writer;
    }

    protected virtual TextWriter PopWriter()
    {
        Output = _textWriterStack.Pop();
        return Output;
    }

    /// <summary>
    /// Write the given value without HTML encoding directly to <see cref="Output"/>.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to write.</param>
    protected void WriteLiteral(object value)
    {
        WriteLiteral(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Write the given value without HTML encoding directly to <see cref="Output"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to write.</param>
    protected void WriteLiteral(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Output.Write(value);
        }
    }

    private List<string>? AttributeValues { get; set; }

    protected void WriteAttributeValue(string thingy, int startPostion, object value, int endValue, int dealyo, bool yesno)
    {
        if (AttributeValues == null)
        {
            AttributeValues = new List<string>();
        }

        AttributeValues.Add(value.ToString()!);
    }

    private string? AttributeEnding { get; set; }

    protected void BeginWriteAttribute(string name, string beginning, int startPosition, string ending, int endPosition, int thingy)
    {
        Debug.Assert(string.IsNullOrEmpty(AttributeEnding));

        Output.Write(beginning);
        AttributeEnding = ending;
    }

    protected void EndWriteAttribute()
    {
        Debug.Assert(AttributeValues != null);
        Debug.Assert(!string.IsNullOrEmpty(AttributeEnding));

        var attributes = string.Join(' ', AttributeValues);
        Output.Write(attributes);
        AttributeValues = null;

        Output.Write(AttributeEnding);
        AttributeEnding = null;
    }

    /// <summary>
    /// Writes the given attribute to the given writer
    /// </summary>
    /// <param name="name">The name of the attribute to write</param>
    /// <param name="leader">The value of the prefix</param>
    /// <param name="trailer">The value of the suffix</param>
    /// <param name="values">The <see cref="AttributeValue"/>s to write.</param>
    protected void WriteAttribute(
        string name,
        string leader,
        string trailer,
        params AttributeValue[] values)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(leader);
        ArgumentNullException.ThrowIfNull(trailer);

        WriteLiteral(leader);
        foreach (var value in values)
        {
            WriteLiteral(value.Prefix);

            // The special cases here are that the value we're writing might already be a string, or that the
            // value might be a bool. If the value is the bool 'true' we want to write the attribute name
            // instead of the string 'true'. If the value is the bool 'false' we don't want to write anything.
            // Otherwise the value is another object (perhaps an HtmlString) and we'll ask it to format itself.
            string? stringValue;
            if (value.Value is bool flag)
            {
                if (flag)
                {
                    stringValue = name;
                }
                else
                {
                    continue;
                }
            }
            else
            {
                stringValue = value.Value as string;
            }

            // Call the WriteTo(string) overload when possible
            if (value.Literal && stringValue != null)
            {
                WriteLiteral(stringValue);
            }
            else if (value.Literal)
            {
                WriteLiteral(value.Value);
            }
            else if (stringValue != null)
            {
                Write(stringValue);
            }
            else
            {
                Write(value.Value);
            }
        }
        WriteLiteral(trailer);
    }

    /// <summary>
    /// <see cref="HelperResult.WriteTo(TextWriter)"/> is invoked
    /// </summary>
    /// <param name="result">The <see cref="HelperResult"/> to invoke</param>
    protected void Write(HelperResult result)
    {
        result.WriteTo(Output);
    }

    /// <summary>
    /// Writes the specified <paramref name="value"/> to <see cref="Output"/>.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to write.</param>
    /// <remarks>
    /// <see cref="HelperResult.WriteTo(TextWriter)"/> is invoked for <see cref="HelperResult"/> types.
    /// For all other types, the encoded result of <see cref="object.ToString"/> is written to
    /// <see cref="Output"/>.
    /// </remarks>
    protected void Write(object value)
    {
        if (value is HelperResult helperResult)
        {
            helperResult.WriteTo(Output);
        }
        else
        {
            Write(Convert.ToString(value, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Writes the specified <paramref name="value"/> with HTML encoding to <see cref="Output"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to write.</param>
    protected void Write(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            WriteLiteral(HtmlEncoder.Encode(value));
        }
    }

    protected string HtmlEncodeAndReplaceLineBreaks(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Split on line breaks before passing it through the encoder.
        return string.Join("<br />" + Environment.NewLine,
            input.Split("\r\n", StringSplitOptions.None)
            .SelectMany(s => s.Split(NewLineChars, StringSplitOptions.None))
            .Select(HtmlEncoder.Encode));
    }
}
