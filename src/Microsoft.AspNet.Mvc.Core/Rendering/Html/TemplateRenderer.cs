using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal class TemplateRenderer
    {
        private static readonly string DisplayTemplateViewPath = "DisplayTemplates";
        private static readonly string EditorTemplateViewPath = "EditorTemplates";

        private ViewContext _viewContext;
        private ViewDataDictionary _viewData;
        private IViewEngine _viewEngine;
        private string _templateName;
        private bool _readOnly;

        public TemplateRenderer([NotNull] IViewEngine viewEngine, 
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

                // Forcing synchronous behavior so users don't have to await templates.
                var viewEngineResult = _viewEngine.FindPartialView(_viewContext.ViewEngineContext, fullViewName);
                if (viewEngineResult.Success)
                {
                    using (var writer = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        // Forcing synchronous behavior so users don't have to await templates.
                        // TODO: Pass through TempData once implemented.
                        viewEngineResult.View.RenderAsync(new ViewContext(_viewContext)
                        {
                            ViewData = _viewData,
                            Writer = writer,
                        }).Wait();

                        return writer.ToString();
                    }
                }

                Func<IHtmlHelper<object>, Task<string>> defaultAction;
                if (defaultActions.TryGetValue(viewName, out defaultAction))
                {
                    // Right now there's no IhtmlHelper<object> pass in or default templates so this will be
                    // changed once a decision has been reached.
                    return defaultAction(null).Result;
                }
            }

            throw new InvalidOperationException(Resources.FormatTemplateHelpers_NoTemplate(_viewData.ModelMetadata.RealModelType.FullName));
        }

        private Dictionary<string, Func<IHtmlHelper<object>, Task<string>>> GetDefaultActions()
        {
            // TODO: Implement default templates
            return new Dictionary<string, Func<IHtmlHelper<object>, Task<string>>>(StringComparer.OrdinalIgnoreCase);
        }

        private IEnumerable<string> GetViewNames()
        {
            var metadata = _viewData.ModelMetadata;
            var templateHints = new string[] {
                _templateName, 
                metadata.TemplateHint, 
                metadata.DataTypeName
            };

            foreach (string templateHint in templateHints.Where(s => !string.IsNullOrEmpty(s)))
            {
                yield return templateHint;
            }

            // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and Nullable<T>)
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
                bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(fieldType);

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
