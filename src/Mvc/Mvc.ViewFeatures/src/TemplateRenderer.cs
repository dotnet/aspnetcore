// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal sealed class TemplateRenderer
{
    private const string DisplayTemplateViewPath = "DisplayTemplates";
    private const string EditorTemplateViewPath = "EditorTemplates";
    public const string IEnumerableOfIFormFileName = "IEnumerable`" + nameof(IFormFile);

    private static readonly Dictionary<string, Func<IHtmlHelper, IHtmlContent>> _defaultDisplayActions =
        new Dictionary<string, Func<IHtmlHelper, IHtmlContent>>(StringComparer.OrdinalIgnoreCase)
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

    private static readonly Dictionary<string, Func<IHtmlHelper, IHtmlContent>> _defaultEditorActions =
        new Dictionary<string, Func<IHtmlHelper, IHtmlContent>>(StringComparer.OrdinalIgnoreCase)
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
                { "DateTime", DefaultEditorTemplates.DateTimeLocalInputTemplate },
                { "DateTime-local", DefaultEditorTemplates.DateTimeLocalInputTemplate },
                { nameof(DateTimeOffset), DefaultEditorTemplates.DateTimeOffsetTemplate },
                { "Time", DefaultEditorTemplates.TimeInputTemplate },
                { "Month", DefaultEditorTemplates.MonthInputTemplate },
                { "Week", DefaultEditorTemplates.WeekInputTemplate },
                { typeof(byte).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(sbyte).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(short).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(ushort).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(int).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(uint).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(long).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(ulong).Name, DefaultEditorTemplates.NumberInputTemplate },
                { typeof(bool).Name, DefaultEditorTemplates.BooleanTemplate },
                { typeof(decimal).Name, DefaultEditorTemplates.DecimalTemplate },
                { typeof(string).Name, DefaultEditorTemplates.StringTemplate },
                { typeof(object).Name, DefaultEditorTemplates.ObjectTemplate },
                { typeof(IFormFile).Name, DefaultEditorTemplates.FileInputTemplate },
                { IEnumerableOfIFormFileName, DefaultEditorTemplates.FileCollectionInputTemplate },
        };

    private readonly IViewEngine _viewEngine;
    private readonly IViewBufferScope _bufferScope;
    private readonly ViewContext _viewContext;
    private readonly ViewDataDictionary _viewData;
    private readonly string _templateName;
    private readonly bool _readOnly;

    public TemplateRenderer(
        IViewEngine viewEngine,
        IViewBufferScope bufferScope,
        ViewContext viewContext,
        ViewDataDictionary viewData,
        string templateName,
        bool readOnly)
    {
        ArgumentNullException.ThrowIfNull(viewEngine);
        ArgumentNullException.ThrowIfNull(bufferScope);
        ArgumentNullException.ThrowIfNull(viewContext);
        ArgumentNullException.ThrowIfNull(viewData);

        _viewEngine = viewEngine;
        _bufferScope = bufferScope;
        _viewContext = viewContext;
        _viewData = viewData;
        _templateName = templateName;
        _readOnly = readOnly;
    }

    public IHtmlContent Render()
    {
        var defaultActions = GetDefaultActions();
        var modeViewPath = _readOnly ? DisplayTemplateViewPath : EditorTemplateViewPath;

        foreach (var viewName in GetViewNames())
        {
            var viewEngineResult = _viewEngine.GetView(_viewContext.ExecutingFilePath, viewName, isMainPage: false);
            if (!viewEngineResult.Success)
            {
                var fullViewName = modeViewPath + "/" + viewName;
                viewEngineResult = _viewEngine.FindView(_viewContext, fullViewName, isMainPage: false);
            }

            if (viewEngineResult.Success)
            {
                var viewBuffer = new ViewBuffer(_bufferScope, viewName, ViewBuffer.PartialViewPageSize);
                using (var writer = new ViewBufferTextWriter(viewBuffer, _viewContext.Writer.Encoding))
                {
                    // Forcing synchronous behavior so users don't have to await templates.
                    var view = viewEngineResult.View;
                    using (view as IDisposable)
                    {
                        var viewContext = new ViewContext(_viewContext, viewEngineResult.View, _viewData, writer);
                        var renderTask = viewEngineResult.View.RenderAsync(viewContext);
                        renderTask.GetAwaiter().GetResult();
                        return viewBuffer;
                    }
                }
            }

            if (defaultActions.TryGetValue(viewName, out var defaultAction))
            {
                return defaultAction(MakeHtmlHelper(_viewContext, _viewData));
            }
        }

        throw new InvalidOperationException(
            Resources.FormatTemplateHelpers_NoTemplate(_viewData.ModelExplorer.ModelType.FullName));
    }

    private Dictionary<string, Func<IHtmlHelper, IHtmlContent>> GetDefaultActions()
    {
        return _readOnly ? _defaultDisplayActions : _defaultEditorActions;
    }

    private IEnumerable<string> GetViewNames()
    {
        var metadata = _viewData.ModelMetadata;
        var templateHints = new[]
        {
                _templateName,
                metadata.TemplateHint,
                metadata.DataTypeName
            };

        foreach (var templateHint in templateHints.Where(s => !string.IsNullOrEmpty(s)))
        {
            yield return templateHint;
        }

        // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and
        // Nullable<T>).
        var fieldType = metadata.UnderlyingOrModelType;
        foreach (var typeName in GetTypeNames(metadata, fieldType))
        {
            yield return typeName;
        }
    }

    public static IEnumerable<string> GetTypeNames(ModelMetadata modelMetadata, Type fieldType)
    {
        // Not returning type name here for IEnumerable<IFormFile> since we will be returning
        // a more specific name, IEnumerableOfIFormFileName.

        if (typeof(IEnumerable<IFormFile>) != fieldType)
        {
            yield return fieldType.Name;
        }

        if (fieldType == typeof(string))
        {
            // Nothing more to provide
            yield break;
        }
        else if (!modelMetadata.IsComplexType)
        {
            // IsEnum is false for the Enum class itself
            if (fieldType.IsEnum)
            {
                // Same as fieldType.BaseType.Name in this case
                yield return "Enum";
            }
            else if (fieldType == typeof(DateTimeOffset))
            {
                yield return "DateTime";
            }

            yield return "String";
            yield break;
        }
        else if (!fieldType.IsInterface)
        {
            var type = fieldType;
            while (true)
            {
                type = type.BaseType;
                if (type == null || type == typeof(object))
                {
                    break;
                }

                yield return type.Name;
            }
        }

        if (typeof(IEnumerable).IsAssignableFrom(fieldType))
        {
            if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(fieldType))
            {
                yield return IEnumerableOfIFormFileName;

                // Specific name has already been returned, now return the generic name.
                if (typeof(IEnumerable<IFormFile>) == fieldType)
                {
                    yield return fieldType.Name;
                }
            }

            yield return "Collection";
        }
        else if (typeof(IFormFile) != fieldType && typeof(IFormFile).IsAssignableFrom(fieldType))
        {
            yield return nameof(IFormFile);
        }

        yield return "Object";
    }

    private static IHtmlHelper MakeHtmlHelper(ViewContext viewContext, ViewDataDictionary viewData)
    {
        var newHelper = viewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper>();

        if (newHelper is IViewContextAware contextable)
        {
            var newViewContext = new ViewContext(viewContext, viewContext.View, viewData, viewContext.Writer);
            contextable.Contextualize(newViewContext);
        }

        return newHelper;
    }
}
