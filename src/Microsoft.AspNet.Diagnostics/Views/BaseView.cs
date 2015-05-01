// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Diagnostics.Views
{
    /// <summary>
    /// Infrastructure
    /// </summary>
    public abstract class BaseView
    {
        /// <summary>
        /// The request context
        /// </summary>
        protected HttpContext Context { get; private set; }

        /// <summary>
        /// The request
        /// </summary>
        protected HttpRequest Request { get; private set; }

        /// <summary>
        /// The response
        /// </summary>
        protected HttpResponse Response { get; private set; }

        /// <summary>
        /// The output stream
        /// </summary>
        protected StreamWriter Output { get; private set; }

        /// <summary>
        /// Html encoder used to encode content.
        /// </summary>
        protected IHtmlEncoder HtmlEncoder { get; set; }

        /// <summary>
        /// Execute an individual request
        /// </summary>
        /// <param name="context"></param>
        public async Task ExecuteAsync(HttpContext context)
        {
            Context = context;
            Request = Context.Request;
            Response = Context.Response;
            Output = new StreamWriter(Response.Body);
            HtmlEncoder = context.ApplicationServices.GetHtmlEncoder();
            await ExecuteAsync();
            Output.Dispose();
        }

        /// <summary>
        /// Execute an individual request
        /// </summary>
        public abstract Task ExecuteAsync();

        /// <summary>
        /// Write the given value directly to the output
        /// </summary>
        /// <param name="value"></param>
        protected void WriteLiteral(string value)
        {
            WriteLiteralTo(Output, value);
        }

        /// <summary>
        /// Write the given value directly to the output
        /// </summary>
        /// <param name="value"></param>
        protected void WriteLiteral(object value)
        {
            WriteLiteralTo(Output, value);
        }

        /// <summary>
        /// Writes the given attribute to the output
        /// </summary>
        /// <param name="name">The name of the attribute to write</param>
        /// <param name="leader">The value and position of the prefix</param>
        /// <param name="trailer">The value and position of the suffix</param>
        /// <param name="values">The <see cref="AttributeValue"/>s to write.</param>
        protected void WriteAttribute(
            [NotNull] string name,
            [NotNull] Tuple<string, int> leader,
            [NotNull] Tuple<string, int> trailer,
            params AttributeValue[] values)
        {
            WriteAttributeTo(
                Output,
                name,
                leader,
                trailer,
                values);
        }

        /// <summary>
        /// Writes the given attribute to the given writer
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="name">The name of the attribute to write</param>
        /// <param name="leader">The value and position of the prefix</param>
        /// <param name="trailer">The value and position of the suffix</param>
        /// <param name="values">The <see cref="AttributeValue"/>s to write.</param>
        protected void WriteAttributeTo(
            [NotNull] TextWriter writer,
            [NotNull] string name,
            [NotNull] Tuple<string, int> leader,
            [NotNull] Tuple<string, int> trailer,
            params AttributeValue[] values)
        {

            WriteLiteralTo(writer, leader.Item1);
            foreach (var value in values)
            {
                WriteLiteralTo(writer, value.Prefix.Item1);

                // The special cases here are that the value we're writing might already be a string, or that the
                // value might be a bool. If the value is the bool 'true' we want to write the attribute name
                // instead of the string 'true'. If the value is the bool 'false' we don't want to write anything.
                // Otherwise the value is another object (perhaps an HtmlString) and we'll ask it to format itself.
                string stringValue;
                if (value.Value.Item1 is bool)
                {
                    if ((bool)value.Value.Item1)
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
                    stringValue = value.Value.Item1 as string;
                }

                // Call the WriteTo(string) overload when possible
                if (value.Literal && stringValue != null)
                {
                    WriteLiteralTo(writer, stringValue);
                }
                else if (value.Literal)
                {
                    WriteLiteralTo(writer, value.Value.Item1);
                }
                else if (stringValue != null)
                {
                    WriteTo(writer, stringValue);
                }
                else
                {
                    WriteTo(writer, value.Value.Item1);
                }
            }
            WriteLiteralTo(writer, trailer.Item1);
        }

        /// <summary>
        /// Convert to string and html encode
        /// </summary>
        /// <param name="value"></param>
        protected void Write(object value)
        {
            WriteTo(Output, value);
        }

        /// <summary>
        /// Html encode and write
        /// </summary>
        /// <param name="value"></param>
        protected void Write(string value)
        {
            WriteTo(Output, value);
        }

        /// <summary>
        /// <see cref="HelperResult.WriteTo(TextWriter)"/> is invoked
        /// </summary>
        /// <param name="result">The <see cref="HelperResult"/> to invoke</param>
        protected void Write(HelperResult result)
        {
            WriteTo(Output, result);
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        /// <remarks>
        /// <see cref="HelperResult.WriteTo(TextWriter)"/> is invoked for <see cref="HelperResult"/> types.
        /// For all other types, the encoded result of <see cref="object.ToString"/> is written to the 
        /// <paramref name="writer"/>.
        /// </remarks>
        protected void WriteTo(TextWriter writer, object value)
        {
            if (value != null)
            {
                var helperResult = value as HelperResult;
                if (helperResult != null)
                {
                    helperResult.WriteTo(writer);
                }
                else
                {
                    WriteTo(writer, Convert.ToString(value, CultureInfo.InvariantCulture));
                }
            }
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="string"/> to write.</param>
        protected void WriteTo(TextWriter writer, string value)
        {
            WriteLiteralTo(writer, HtmlEncoder.HtmlEncode(value));
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        protected void WriteLiteralTo(TextWriter writer, object value)
        {
            WriteLiteralTo(writer, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to write.</param>
        protected void WriteLiteralTo(TextWriter writer, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.Write(value);
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
                input.Split(new[] { "\r\n" }, StringSplitOptions.None)
                .SelectMany(s => s.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
                .Select(HtmlEncoder.HtmlEncode));
        }
    }
}