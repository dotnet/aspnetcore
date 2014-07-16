// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    public abstract class RazorView : IView
    {
        private readonly HashSet<string> _renderedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _renderedBody;

        [Activate]
        public IUrlHelper Url { get; set; }

        public HttpContext Context
        {
            get
            {
                if (ViewContext == null)
                {
                    return null;
                }

                return ViewContext.HttpContext;
            }
        }

        public ViewContext ViewContext { get; set; }

        public string Layout { get; set; }

        protected TextWriter Output { get; set; }

        public virtual IPrincipal User
        {
            get
            {
                if (Context == null)
                {
                    return null;
                }

                return Context.User;
            }
        }

        public dynamic ViewBag
        {
            get
            {
                return (ViewContext == null) ? null : ViewContext.ViewBag;
            }
        }

        private string BodyContent { get; set; }

        private Dictionary<string, HelperResult> SectionWriters { get; set; }

        private Dictionary<string, HelperResult> PreviousSectionWriters { get; set; }

        public virtual async Task RenderAsync([NotNull] ViewContext context)
        {
            SectionWriters = new Dictionary<string, HelperResult>(StringComparer.OrdinalIgnoreCase);
            ViewContext = context;

            var contentBuilder = new StringBuilder(1024);
            using (var bodyWriter = new StringWriter(contentBuilder))
            {
                Output = bodyWriter;

                // The writer for the body is passed through the ViewContext, allowing things like HtmlHelpers
                // and ViewComponents to reference it.
                var oldWriter = context.Writer;

                try
                {
                    context.Writer = bodyWriter;
                    await ExecuteAsync();

                    // Verify that RenderBody is called, or that RenderSection is called for all sections
                    VerifyRenderedBodyOrSections();
                }
                finally
                {
                    context.Writer = oldWriter;
                }
            }

            var bodyContent = contentBuilder.ToString();
            if (!string.IsNullOrEmpty(Layout))
            {
                await RenderLayoutAsync(context, bodyContent);
            }
            else
            {
                await context.Writer.WriteAsync(bodyContent);
            }
        }

        private async Task RenderLayoutAsync(ViewContext context, string bodyContent)
        {
            var virtualPathFactory = context.HttpContext.RequestServices.GetService<IVirtualPathViewFactory>();
            var layoutView = (RazorView)virtualPathFactory.CreateInstance(Layout);

            if (layoutView == null)
            {
                var message = Resources.FormatLayoutCannotBeLocated(Layout);
                throw new InvalidOperationException(message);
            }

            layoutView.PreviousSectionWriters = SectionWriters;
            layoutView.BodyContent = bodyContent;
            await layoutView.RenderAsync(context);
        }

        public abstract Task ExecuteAsync();

        public virtual void Write(object value)
        {
            WriteTo(Output, value);
        }

        public virtual void WriteTo(TextWriter writer, object content)
        {
            if (content != null)
            {
                var helperResult = content as HelperResult;
                if (helperResult != null)
                {
                    helperResult.WriteTo(writer);
                }
                else
                {
                    var htmlString = content as HtmlString;
                    if (htmlString != null)
                    {
                        writer.Write(content.ToString());
                    }
                    else
                    {
                        writer.Write(WebUtility.HtmlEncode(content.ToString()));
                    }
                }
            }
        }

        public void WriteLiteral(object value)
        {
            WriteLiteralTo(Output, value);
        }

        public virtual void WriteLiteralTo(TextWriter writer, object text)
        {
            if (text != null)
            {
                writer.Write(text.ToString());
            }
        }

        public virtual void WriteAttribute(string name,
                                           PositionTagged<string> prefix,
                                           PositionTagged<string> suffix,
                                           params AttributeValue[] values)
        {
            WriteAttributeTo(Output, name, prefix, suffix, values);
        }

        public virtual void WriteAttributeTo(TextWriter writer,
                                             string name,
                                             PositionTagged<string> prefix,
                                             PositionTagged<string> suffix,
                                             params AttributeValue[] values)
        {
            var first = true;
            var wroteSomething = false;
            if (values.Length == 0)
            {
                // Explicitly empty attribute, so write the prefix and suffix
                WritePositionTaggedLiteral(writer, prefix);
                WritePositionTaggedLiteral(writer, suffix);
            }
            else
            {
                for (var i = 0; i < values.Length; i++)
                {
                    var attrVal = values[i];
                    var val = attrVal.Value;
                    var next = i == values.Length - 1 ?
                        suffix : // End of the list, grab the suffix
                        values[i + 1].Prefix; // Still in the list, grab the next prefix

                    if (val.Value == null)
                    {
                        // Nothing to write
                        continue;
                    }

                    // The special cases here are that the value we're writing might already be a string, or that the 
                    // value might be a bool. If the value is the bool 'true' we want to write the attribute name
                    // instead of the string 'true'. If the value is the bool 'false' we don't want to write anything.
                    // Otherwise the value is another object (perhaps an HtmlString) and we'll ask it to format itself.
                    string stringValue;
                    var boolValue = val.Value as bool?;
                    if (boolValue == true)
                    {
                        stringValue = name;
                    }
                    else if (boolValue == false)
                    {
                        continue;
                    }
                    else
                    {
                        stringValue = val.Value as string;
                    }

                    if (first)
                    {
                        WritePositionTaggedLiteral(writer, prefix);
                        first = false;
                    }
                    else
                    {
                        WritePositionTaggedLiteral(writer, attrVal.Prefix);
                    }

                    // Calculate length of the source span by the position of the next value (or suffix)
                    var sourceLength = next.Position - attrVal.Value.Position;

                    if (attrVal.Literal)
                    {
                        WriteLiteralTo(writer, stringValue ?? val.Value);
                    }
                    else
                    {
                        WriteTo(writer, stringValue ?? val.Value); // Write value
                    }
                    wroteSomething = true;
                }
                if (wroteSomething)
                {
                    WritePositionTaggedLiteral(writer, suffix);
                }
            }
        }

        public virtual string Href([NotNull] string contentPath)
        {
            return Url.Content(contentPath);
        }

        private void WritePositionTaggedLiteral(TextWriter writer, string value, int position)
        {
            WriteLiteralTo(writer, value);
        }

        private void WritePositionTaggedLiteral(TextWriter writer, PositionTagged<string> value)
        {
            WritePositionTaggedLiteral(writer, value.Value, value.Position);
        }

        protected virtual HtmlString RenderBody()
        {
            if (BodyContent == null)
            {
                throw new InvalidOperationException(Resources.FormatRenderBodyCannotBeCalled("RenderBody"));
            }
            _renderedBody = true;
            return new HtmlString(BodyContent);
        }

        public void DefineSection(string name, HelperResult action)
        {
            if (SectionWriters.ContainsKey(name))
            {
                throw new InvalidOperationException(Resources.FormatSectionAlreadyDefined(name));
            }
            SectionWriters[name] = action;
        }

        public bool IsSectionDefined([NotNull] string name)
        {
            EnsureMethodCanBeInvoked("IsSectionDefined");
            return PreviousSectionWriters.ContainsKey(name);
        }

        public HelperResult RenderSection([NotNull] string name)
        {
            return RenderSection(name, required: true);
        }

        public HelperResult RenderSection([NotNull] string name, bool required)
        {
            EnsureMethodCanBeInvoked("RenderSection");
            if (_renderedSections.Contains(name))
            {
                throw new InvalidOperationException(Resources.FormatSectionAlreadyRendered("RenderSection", name));
            }

            HelperResult action;
            if (PreviousSectionWriters.TryGetValue(name, out action))
            {
                _renderedSections.Add(name);
                return action;
            }
            else if (required)
            {
                // If the section is not found, and it is not optional, throw an error.
                throw new InvalidOperationException(Resources.FormatSectionNotDefined(name));
            }
            else
            {
                // If the section is optional and not found, then don't do anything.
                return null;
            }
        }

        private void EnsureMethodCanBeInvoked(string methodName)
        {
            if (PreviousSectionWriters == null)
            {
                throw new InvalidOperationException(Resources.FormatView_MethodCannotBeCalled(methodName));
            }
        }

        private void VerifyRenderedBodyOrSections()
        {
            if (BodyContent != null)
            {
                var sectionsNotRendered = PreviousSectionWriters.Keys.Except(_renderedSections,
                                                                             StringComparer.OrdinalIgnoreCase);
                if (sectionsNotRendered.Any())
                {
                    var sectionNames = String.Join(", ", sectionsNotRendered);
                    throw new InvalidOperationException(Resources.FormatSectionsNotRendered(sectionNames));
                }
                else if (!_renderedBody)
                {
                    // If a body was defined, then RenderBody should have been called.
                    throw new InvalidOperationException(Resources.FormatRenderBodyNotCalled("RenderBody"));
                }
            }
        }
    }
}