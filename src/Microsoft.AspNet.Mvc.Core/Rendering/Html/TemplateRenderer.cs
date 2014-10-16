// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal class TemplateRenderer
    {
        private static readonly string DisplayTemplateViewPath = "DisplayTemplates";
        private static readonly string EditorTemplateViewPath = "EditorTemplates";

        private static readonly Dictionary<string, Func<IHtmlHelper, string>> _defaultDisplayActions =
            new Dictionary<string, Func<IHtmlHelper, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Collection", DefaultDisplayTemplates.CollectionTemplate },
                { "EmailAddress", DefaultDisplayTemplates.EmailAddressTemplate },
                { "HiddenInput", DefaultDisplayTemplates.HiddenInputTemplate },
                { "Html", DefaultDisplayTemplates.HtmlTemplate },
                { "Text", DefaultDisplayTemplates.StringTemplate },
                { "Url", DefaultDisplayTemplates.UrlTemplate },
                { typeof(bool).Name, DefaultDisplayTemplates.BooleanTemplate },
                { typeof(decimal).Name, DefaultDisplayTemplates.DecimalTemplate },
                { typeof(string).Name, DefaultDisplayTemplates.StringTemplate },
                { typeof(object).Name, DefaultDisplayTemplates.ObjectTemplate },
            };

        private static readonly Dictionary<string, Func<IHtmlHelper, string>> _defaultEditorActions =
            new Dictionary<string, Func<IHtmlHelper, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Collection", DefaultEditorTemplates.CollectionTemplate },
                { "EmailAddress", DefaultEditorTemplates.EmailAddressInputTemplate },
                { "HiddenInput", DefaultEditorTemplates.HiddenInputTemplate },
                { "MultilineText", DefaultEditorTemplates.MultilineTemplate },
                { "Password", DefaultEditorTemplates.PasswordTemplate },
                { "PhoneNumber", DefaultEditorTemplates.PhoneNumberInputTemplate },
                { "Text", DefaultEditorTemplates.StringTemplate },
                { "Url", DefaultEditorTemplates.UrlInputTemplate },
                { "Date", DefaultEditorTemplates.DateInputTemplate },
                { "DateTime", DefaultEditorTemplates.DateTimeInputTemplate },
                { "DateTime-local", DefaultEditorTemplates.DateTimeLocalInputTemplate },
                { "Time", DefaultEditorTemplates.TimeInputTemplate },
                { typeof(byte).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(sbyte).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(int).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(uint).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(long).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(ulong).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(bool).Name, DefaultEditorTemplates.BooleanTemplate },
                { typeof(decimal).Name, DefaultEditorTemplates.DecimalTemplate },
                { typeof(string).Name, DefaultEditorTemplates.StringTemplate },
                { typeof(object).Name, DefaultEditorTemplates.ObjectTemplate },
            };

        private ViewContext _viewContext;
        private ViewDataDictionary _viewData;
        private IViewEngine _viewEngine;
        private string _templateName;
        private bool _readOnly;

        public TemplateRenderer(
            [NotNull] IViewEngine viewEngine,
            [NotNull] ViewContext viewContext,
            [NotNull] ViewDataDictionary viewData,
            string templateName,
            bool readOnly)
        {
            _viewEngine = viewEngine;
            _viewContext = viewContext;
            _viewData = viewData;
            _templateName = templateName;
            _readOnly = readOnly;
        }

        public string Render()
        {
            var defaultActions = GetDefaultActions();
            var modeViewPath = _readOnly ? DisplayTemplateViewPath : EditorTemplateViewPath;

            foreach (string viewName in GetViewNames())
            {
                var fullViewName = modeViewPath + "/" + viewName;

                var viewEngineResult = _viewEngine.FindPartialView(_viewContext, fullViewName);
                if (viewEngineResult.Success)
                {
                    using (var writer = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        // Forcing synchronous behavior so users don't have to await templates.
                        // TODO: Pass through TempData once implemented.
                        var view = viewEngineResult.View;
                        using (view as IDisposable)
                        {
                            var viewContext = new ViewContext(_viewContext, viewEngineResult.View, _viewData, writer);
                            var renderTask = viewEngineResult.View.RenderAsync(viewContext);
                            TaskHelper.WaitAndThrowIfFaulted(renderTask);
                            return writer.ToString();
                        }
                    }
                }

                Func<IHtmlHelper, string> defaultAction;
                if (defaultActions.TryGetValue(viewName, out defaultAction))
                {
                    return defaultAction(MakeHtmlHelper(_viewContext, _viewData));
                }
            }

            throw new InvalidOperationException(
                Resources.FormatTemplateHelpers_NoTemplate(_viewData.ModelMetadata.RealModelType.FullName));
        }

        private Dictionary<string, Func<IHtmlHelper, string>> GetDefaultActions()
        {
            return _readOnly ? _defaultDisplayActions : _defaultEditorActions;
        }

        private IEnumerable<string> GetViewNames()
        {
            var metadata = _viewData.ModelMetadata;
            var templateHints = new string[]
            {
                _templateName,
                metadata.TemplateHint,
                metadata.DataTypeName
            };

            foreach (string templateHint in templateHints.Where(s => !string.IsNullOrEmpty(s)))
            {
                yield return templateHint;
            }

            // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and
            // Nullable<T>).
            var fieldType = Nullable.GetUnderlyingType(metadata.RealModelType) ?? metadata.RealModelType;

            yield return fieldType.Name;

            if (fieldType == typeof(string))
            {
                // Nothing more to provide
                yield break;
            }
            else if (!metadata.IsComplexType)
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

        private static IHtmlHelper MakeHtmlHelper(ViewContext viewContext, ViewDataDictionary viewData)
        {
            var newHelper = viewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper>();

            var contextable = newHelper as ICanHasViewContext;
            if (contextable != null)
            {
                var newViewContext = new ViewContext(viewContext, viewContext.View, viewData, viewContext.Writer);
                contextable.Contextualize(newViewContext);
            }

            return newHelper;
        }
    }
}
