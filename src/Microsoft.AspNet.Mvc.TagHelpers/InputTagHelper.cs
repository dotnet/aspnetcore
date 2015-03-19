// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;input&gt; elements with an <c>asp-for</c> attribute.
    /// </summary>
    [TargetElement("input", Attributes = ForAttributeName)]
    public class InputTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string FormatAttributeName = "asp-format";

        // Mapping from datatype names and data annotation hints to values for the <input/> element's "type" attribute.
        private static readonly Dictionary<string, string> _defaultInputTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HiddenInput", InputType.Hidden.ToString().ToLowerInvariant() },
                { "Password", InputType.Password.ToString().ToLowerInvariant() },
                { "Text", InputType.Text.ToString().ToLowerInvariant() },
                { "PhoneNumber", "tel" },
                { "Url", "url" },
                { "EmailAddress", "email" },
                { "Date", "date" },
                { "DateTime", "datetime" },
                { "DateTime-local", "datetime-local" },
                { "Time", "time" },
                { nameof(Byte), "number" },
                { nameof(SByte), "number" },
                { nameof(Int32), "number" },
                { nameof(UInt32), "number" },
                { nameof(Int64), "number" },
                { nameof(UInt64), "number" },
                { nameof(Boolean), InputType.CheckBox.ToString().ToLowerInvariant() },
                { nameof(Decimal), InputType.Text.ToString().ToLowerInvariant() },
                { nameof(String), InputType.Text.ToString().ToLowerInvariant() },
            };

        // Mapping from <input/> element's type to RFC 3339 date and time formats.
        private static readonly Dictionary<string, string> _rfc3339Formats =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "date", "{0:yyyy-MM-dd}" },
                { "datetime", "{0:yyyy-MM-ddTHH:mm:ss.fffK}" },
                { "datetime-local", "{0:yyyy-MM-ddTHH:mm:ss.fff}" },
                { "time", "{0:HH:mm:ss.fff}" },
            };

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal IHtmlGenerator Generator { get; set; }

        // Protected to ensure subclasses are correctly activated. Internal for ease of use when testing.
        [Activate]
        protected internal ViewContext ViewContext { get; set; }

        /// <summary>
        /// An expression to be evaluated against the current model.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        /// <summary>
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx) to
        /// apply when converting the <see cref="For"/> result to a <see cref="string"/>. Sets the generated "value"
        /// attribute to that formatted <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// Used only the calculated "type" attribute is "text" (the most common value) e.g.
        /// <see cref="InputTypeName"/> is "String". That is, <see cref="Format"/> is used when calling
        /// <see cref="IHtmlGenerator.GenerateTextBox"/>.
        /// </remarks>
        [HtmlAttributeName(FormatAttributeName)]
        public string Format { get; set; }

        /// <summary>
        /// The type of the &lt;input&gt; element.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases. Also used to determine the <see cref="IHtmlGenerator"/>
        /// helper to call and the default <see cref="Format"/> value (when calling
        /// <see cref="IHtmlGenerator.GenerateTextBox"/>).
        /// </remarks>
        [HtmlAttributeName("type")]
        public string InputTypeName { get; set; }

        /// <summary>
        /// The value of the &lt;input&gt; element.
        /// </summary>
        /// <remarks>
        /// Passed through to the generated HTML in all cases. Also used to determine the generated "checked" attribute
        /// if <see cref="InputTypeName"/> is "radio". Must not be <c>null</c> in that case.
        /// </remarks>
        public string Value { get; set; }

        /// <inheritdoc />
        /// <remarks>Does nothing if <see cref="For"/> is <c>null</c>.</remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Format"/> is non-<c>null</c> but <see cref="For"/> is <c>null</c>.
        /// </exception>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Pass through attributes that are also well-known HTML attributes. Must be done prior to any copying
            // from a TagBuilder.
            if (!string.IsNullOrEmpty(InputTypeName))
            {
                output.CopyHtmlAttribute("type", context);
            }

            if (Value != null)
            {
                output.CopyHtmlAttribute(nameof(Value), context);
            }

            // Note null or empty For.Name is allowed because TemplateInfo.HtmlFieldPrefix may be sufficient.
            // IHtmlGenerator will enforce name requirements.
            var metadata = For.Metadata;
            var modelExplorer = For.ModelExplorer;
            if (metadata == null)
            {
                throw new InvalidOperationException(Resources.FormatTagHelpers_NoProvidedMetadata(
                    "<input>",
                    ForAttributeName,
                    nameof(IModelMetadataProvider),
                    For.Name));
            }

            string inputType;
            string inputTypeHint;
            if (string.IsNullOrEmpty(InputTypeName))
            {
                // Note GetInputType never returns null.
                inputType = GetInputType(modelExplorer, out inputTypeHint);
            }
            else
            {
                inputType = InputTypeName.ToLowerInvariant();
                inputTypeHint = null;
            }

            // inputType may be more specific than default the generator chooses below.
            if (!output.Attributes.ContainsKey("type"))
            {
                output.Attributes["type"] = inputType;
            }

            TagBuilder tagBuilder;
            switch (inputType)
            {
                case "checkbox":
                    GenerateCheckBox(modelExplorer, output);
                    return;

                case "hidden":
                    tagBuilder = Generator.GenerateHidden(
                        ViewContext,
                        modelExplorer,
                        For.Name,
                        value: For.Model,
                        useViewData: false,
                        htmlAttributes: null);
                    break;

                case "password":
                    tagBuilder = Generator.GeneratePassword(
                        ViewContext,
                        modelExplorer,
                        For.Name,
                        value: null,
                        htmlAttributes: null);
                    break;

                case "radio":
                    tagBuilder = GenerateRadio(modelExplorer);
                    break;

                default:
                    tagBuilder = GenerateTextBox(modelExplorer, inputTypeHint, inputType);
                    break;
            }

            if (tagBuilder != null)
            {
                // This TagBuilder contains the one <input/> element of interest. Since this is not the "checkbox"
                // special-case, output is a self-closing element no longer guarunteed.
                output.MergeAttributes(tagBuilder);
                output.Content.Append(tagBuilder.InnerHtml);
            }
        }

        private void GenerateCheckBox(ModelExplorer modelExplorer, TagHelperOutput output)
        {
            if (typeof(bool) != modelExplorer.ModelType)
            {
                throw new InvalidOperationException(Resources.FormatInputTagHelper_InvalidExpressionResult(
                    "<input>",
                    ForAttributeName,
                    modelExplorer.ModelType.FullName,
                    typeof(bool).FullName,
                    "type",
                    "checkbox"));
            }

            // Prepare to move attributes from current element to <input type="checkbox"/> generated just below.
            var htmlAttributes = output.Attributes.ToDictionary(
                attribute => attribute.Key,
                attribute => (object)attribute.Value);

            var tagBuilder = Generator.GenerateCheckBox(
                ViewContext,
                modelExplorer,
                For.Name,
                isChecked: null,
                htmlAttributes: htmlAttributes);
            if (tagBuilder != null)
            {
                // Do not generate current element's attributes or tags. Instead put both <input type="checkbox"/> and
                // <input type="hidden"/> into the output's Content.
                output.Attributes.Clear();
                output.TagName = null;

                output.Content.Append(tagBuilder.ToString(TagRenderMode.SelfClosing));

                tagBuilder = Generator.GenerateHiddenForCheckbox(ViewContext, modelExplorer, For.Name);
                if (tagBuilder != null)
                {
                    output.Content.Append(tagBuilder.ToString(TagRenderMode.SelfClosing));
                }
            }
        }

        private TagBuilder GenerateRadio(ModelExplorer modelExplorer)
        {
            // Note empty string is allowed.
            if (Value == null)
            {
                throw new InvalidOperationException(Resources.FormatInputTagHelper_ValueRequired(
                    "<input>",
                    nameof(Value).ToLowerInvariant(),
                    "type",
                    "radio"));
            }

            return Generator.GenerateRadioButton(
                ViewContext,
                modelExplorer,
                For.Name,
                Value,
                isChecked: null,
                htmlAttributes: null);
        }

        private TagBuilder GenerateTextBox(ModelExplorer modelExplorer, string inputTypeHint, string inputType)
        {
            var format = Format;
            if (string.IsNullOrEmpty(format))
            {
                format = GetFormat(modelExplorer, inputTypeHint, inputType);
            }

            return Generator.GenerateTextBox(
                ViewContext,
                modelExplorer,
                For.Name,
                value: modelExplorer.Model,
                format: Format,
                htmlAttributes: null);
        }

        // Get a fall-back format based on the metadata.
        private string GetFormat(ModelExplorer modelExplorer, string inputTypeHint, string inputType)
        {
            string format;
            string rfc3339Format;
            if (string.Equals("decimal", inputTypeHint, StringComparison.OrdinalIgnoreCase) &&
                string.Equals("text", inputType, StringComparison.Ordinal) &&
                string.IsNullOrEmpty(modelExplorer.Metadata.EditFormatString))
            {
                // Decimal data is edited using an <input type="text"/> element, with a reasonable format.
                // EditFormatString has precedence over this fall-back format.
                format = "{0:0.00}";
            }
            else if (_rfc3339Formats.TryGetValue(inputType, out rfc3339Format) &&
                ViewContext.Html5DateRenderingMode == Html5DateRenderingMode.Rfc3339 &&
                !modelExplorer.Metadata.HasNonDefaultEditFormat &&
                (typeof(DateTime) == modelExplorer.ModelType || typeof(DateTimeOffset) == modelExplorer.ModelType))
            {
                // Rfc3339 mode _may_ override EditFormatString in a limited number of cases e.g. EditFormatString
                // must be a default format (i.e. came from a built-in [DataType] attribute).
                format = rfc3339Format;
            }
            else
            {
                // Otherwise use EditFormatString, if any.
                format = modelExplorer.Metadata.EditFormatString;
            }

            return format;
        }

        private string GetInputType(ModelExplorer modelExplorer, out string inputTypeHint)
        {
            foreach (var hint in GetInputTypeHints(modelExplorer))
            {
                string inputType;
                if (_defaultInputTypes.TryGetValue(hint, out inputType))
                {
                    inputTypeHint = hint;
                    return inputType;
                }
            }

            inputTypeHint = InputType.Text.ToString().ToLowerInvariant();
            return inputTypeHint;
        }

        // A variant of TemplateRenderer.GetViewNames(). Main change relates to bool? handling.
        private static IEnumerable<string> GetInputTypeHints(ModelExplorer modelExplorer)
        {
            var inputTypeHints = new string[]
            {
                modelExplorer.Metadata.TemplateHint,
                modelExplorer.Metadata.DataTypeName,
            };

            foreach (string inputTypeHint in inputTypeHints.Where(s => !string.IsNullOrEmpty(s)))
            {
                yield return inputTypeHint;
            }

            // In most cases, we don't want to search for Nullable<T>. We want to search for T, which should handle
            // both T and Nullable<T>. However we special-case bool? to avoid turning an <input/> into a <select/>.
            var fieldType = modelExplorer.ModelType;
            if (typeof(bool?) != fieldType)
            {
                var underlyingType = Nullable.GetUnderlyingType(fieldType);
                if (underlyingType != null)
                {
                    fieldType = underlyingType;
                }
            }

            yield return fieldType.Name;

            if (fieldType == typeof(string))
            {
                // Nothing more to provide
                yield break;
            }
            else if (!modelExplorer.Metadata.IsComplexType)
            {
                // IsEnum is false for the Enum class itself
                if (fieldType.IsEnum())
                {
                    // Same as fieldType.BaseType.Name in this case
                    yield return "Enum";
                }
                else if (fieldType == typeof(DateTimeOffset))
                {
                    yield return "DateTime";
                }

                yield return "String";
            }
            else if (fieldType.IsInterface())
            {
                if (typeof(IEnumerable).IsAssignableFrom(fieldType))
                {
                    yield return "Collection";
                }

                yield return "Object";
            }
            else
            {
                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(fieldType);
                while (true)
                {
                    fieldType = fieldType.BaseType();
                    if (fieldType == null)
                    {
                        break;
                    }

                    if (isEnumerable && fieldType == typeof(Object))
                    {
                        yield return "Collection";
                    }

                    yield return fieldType.Name;
                }
            }
        }
    }
}