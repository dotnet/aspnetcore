// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed partial class DefaultWebAssemblyJSRuntime
{
    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;

    internal Renderer? Renderer { get; set; }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static Task DispatchLocationChanged(string uri, string? state, bool isInterceptedLink)
    {
        return ScheduleOnCallQueue(
            (uri, state, isInterceptedLink),
            static s => WebAssemblyNavigationManager.Instance.SetLocation(s.uri, s.state, s.isInterceptedLink));
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static Task<bool> DispatchLocationChanging(string uri, string? state, bool isInterceptedLink)
    {
        return ScheduleOnCallQueue<(string uri, string? state, bool isInterceptedLink), bool>(
            (uri, state, isInterceptedLink),
            static s => WebAssemblyNavigationManager.Instance.HandleLocationChangingAsync(s.uri, s.state, s.isInterceptedLink).AsTask());
    }

    [SupportedOSPlatform("browser")]
    [JSExport]
    public static Task UpdateRootComponents(string operationsJson, string appState)
    {
        return ScheduleOnCallQueue((operationsJson, appState), static s =>
        {
            try
            {
                var operations = DeserializeOperations(s.operationsJson);
                Instance.OnUpdateRootComponents?.Invoke(operations, s.appState);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error in {nameof(UpdateRootComponents)}: {ex}");
            }
        });
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchMouseEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        [JSMarshalAs<JSType.Number>] long detail,
        double screenX, double screenY,
        double clientX, double clientY,
        double offsetX, double offsetY,
        double pageX, double pageY,
        double movementX, double movementY,
        [JSMarshalAs<JSType.Number>] long button,
        [JSMarshalAs<JSType.Number>] long buttons,
        bool ctrlKey, bool shiftKey, bool altKey, bool metaKey,
        string type)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new MouseEventArgs
        {
            Detail = detail,
            ScreenX = screenX, ScreenY = screenY,
            ClientX = clientX, ClientY = clientY,
            OffsetX = offsetX, OffsetY = offsetY,
            PageX = pageX, PageY = pageY,
            MovementX = movementX, MovementY = movementY,
            Button = button, Buttons = buttons,
            CtrlKey = ctrlKey, ShiftKey = shiftKey, AltKey = altKey, MetaKey = metaKey,
            Type = type,
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchDragEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        [JSMarshalAs<JSType.Number>] long detail,
        double screenX, double screenY,
        double clientX, double clientY,
        double offsetX, double offsetY,
        double pageX, double pageY,
        double movementX, double movementY,
        [JSMarshalAs<JSType.Number>] long button,
        [JSMarshalAs<JSType.Number>] long buttons,
        bool ctrlKey, bool shiftKey, bool altKey, bool metaKey,
        string type,
        string? dropEffect, string? effectAllowed,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[]? files,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[]? itemKinds,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[]? itemTypes,
        [JSMarshalAs<JSType.Array<JSType.String>>] string[]? types)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new DragEventArgs
        {
            Detail = detail,
            ScreenX = screenX, ScreenY = screenY,
            ClientX = clientX, ClientY = clientY,
            OffsetX = offsetX, OffsetY = offsetY,
            PageX = pageX, PageY = pageY,
            MovementX = movementX, MovementY = movementY,
            Button = button, Buttons = buttons,
            CtrlKey = ctrlKey, ShiftKey = shiftKey, AltKey = altKey, MetaKey = metaKey,
            Type = type,
            DataTransfer = new DataTransfer
            {
                DropEffect = dropEffect ?? string.Empty,
                EffectAllowed = effectAllowed,
                Files = files ?? [],
                Items = UnflattenDataTransferItems(itemKinds, itemTypes),
                Types = types ?? [],
            },
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchKeyboardEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        string key, string code,
        float location,
        bool repeat,
        bool ctrlKey, bool shiftKey, bool altKey, bool metaKey,
        string type,
        bool isComposing)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new KeyboardEventArgs
        {
            Key = key,
            Code = code,
            Location = location,
            Repeat = repeat,
            CtrlKey = ctrlKey, ShiftKey = shiftKey, AltKey = altKey, MetaKey = metaKey,
            Type = type,
            IsComposing = isComposing,
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchChangeEventString(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        string value)
    {
        EventFieldInfo? fieldInfo = fieldComponentId >= 0
            ? new EventFieldInfo { ComponentId = fieldComponentId, FieldValue = fieldValueString! }
            : null;
        var eventArgs = new ChangeEventArgs { Value = value };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchChangeEventBool(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        bool fieldValueBool,
        bool value)
    {
        EventFieldInfo? fieldInfo = fieldComponentId >= 0
            ? new EventFieldInfo { ComponentId = fieldComponentId, FieldValue = fieldValueBool ? BoxedTrue : BoxedFalse }
            : null;
        var eventArgs = new ChangeEventArgs { Value = value };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchChangeEventStringArray(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        [JSMarshalAs<JSType.Array<JSType.String>>] string?[] value)
    {
        EventFieldInfo? fieldInfo = fieldComponentId >= 0
            ? new EventFieldInfo { ComponentId = fieldComponentId, FieldValue = fieldValueString! }
            : null;
        var eventArgs = new ChangeEventArgs { Value = value };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchFocusEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        string? type)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new FocusEventArgs { Type = type };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchClipboardEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        string type)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new ClipboardEventArgs { Type = type };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchPointerEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        [JSMarshalAs<JSType.Number>] long detail,
        double screenX, double screenY,
        double clientX, double clientY,
        double offsetX, double offsetY,
        double pageX, double pageY,
        double movementX, double movementY,
        [JSMarshalAs<JSType.Number>] long button,
        [JSMarshalAs<JSType.Number>] long buttons,
        bool ctrlKey, bool shiftKey, bool altKey, bool metaKey,
        string type,
        [JSMarshalAs<JSType.Number>] long pointerId,
        float width, float height,
        float pressure,
        float tiltX, float tiltY,
        string pointerType,
        bool isPrimary)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new PointerEventArgs
        {
            Detail = detail,
            ScreenX = screenX, ScreenY = screenY,
            ClientX = clientX, ClientY = clientY,
            OffsetX = offsetX, OffsetY = offsetY,
            PageX = pageX, PageY = pageY,
            MovementX = movementX, MovementY = movementY,
            Button = button, Buttons = buttons,
            CtrlKey = ctrlKey, ShiftKey = shiftKey, AltKey = altKey, MetaKey = metaKey,
            Type = type,
            PointerId = pointerId,
            Width = width, Height = height,
            Pressure = pressure,
            TiltX = tiltX, TiltY = tiltY,
            PointerType = pointerType,
            IsPrimary = isPrimary,
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchWheelEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        [JSMarshalAs<JSType.Number>] long detail,
        double screenX, double screenY,
        double clientX, double clientY,
        double offsetX, double offsetY,
        double pageX, double pageY,
        double movementX, double movementY,
        [JSMarshalAs<JSType.Number>] long button,
        [JSMarshalAs<JSType.Number>] long buttons,
        bool ctrlKey, bool shiftKey, bool altKey, bool metaKey,
        string type,
        double deltaX, double deltaY, double deltaZ,
        [JSMarshalAs<JSType.Number>] long deltaMode)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new WheelEventArgs
        {
            Detail = detail,
            ScreenX = screenX, ScreenY = screenY,
            ClientX = clientX, ClientY = clientY,
            OffsetX = offsetX, OffsetY = offsetY,
            PageX = pageX, PageY = pageY,
            MovementX = movementX, MovementY = movementY,
            Button = button, Buttons = buttons,
            CtrlKey = ctrlKey, ShiftKey = shiftKey, AltKey = altKey, MetaKey = metaKey,
            Type = type,
            DeltaX = deltaX, DeltaY = deltaY, DeltaZ = deltaZ,
            DeltaMode = deltaMode,
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchTouchEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        [JSMarshalAs<JSType.Number>] long detail,
        [JSMarshalAs<JSType.Array<JSType.Number>>] double[]? touchesFlat,
        [JSMarshalAs<JSType.Array<JSType.Number>>] double[]? targetTouchesFlat,
        [JSMarshalAs<JSType.Array<JSType.Number>>] double[]? changedTouchesFlat,
        bool ctrlKey, bool shiftKey, bool altKey, bool metaKey,
        string type)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new TouchEventArgs
        {
            Detail = detail,
            Touches = UnflattenTouchPoints(touchesFlat),
            TargetTouches = UnflattenTouchPoints(targetTouchesFlat),
            ChangedTouches = UnflattenTouchPoints(changedTouchesFlat),
            CtrlKey = ctrlKey, ShiftKey = shiftKey, AltKey = altKey, MetaKey = metaKey,
            Type = type,
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchProgressEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        bool lengthComputable,
        [JSMarshalAs<JSType.Number>] long loaded,
        [JSMarshalAs<JSType.Number>] long total,
        string type)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new ProgressEventArgs
        {
            LengthComputable = lengthComputable,
            Loaded = loaded,
            Total = total,
            Type = type,
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchErrorEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        string? message, string? filename,
        int lineno, int colno,
        string? type)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var eventArgs = new Microsoft.AspNetCore.Components.Web.ErrorEventArgs
        {
            Message = message,
            Filename = filename,
            Lineno = lineno,
            Colno = colno,
            Type = type,
        };
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static void DispatchEmptyEvent(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        DispatchEventCore(eventHandlerId, fieldInfo, EventArgs.Empty);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "Custom event arg types are preserved by the component that declares the event handler.")]
    internal static void DispatchEventJson(
        [JSMarshalAs<JSType.Number>] long eventHandlerId,
        int fieldComponentId,
        string? fieldValueString,
        bool fieldValueBool,
        string eventName,
        string eventArgsJson)
    {
        var fieldInfo = CreateFieldInfo(fieldComponentId, fieldValueString, fieldValueBool);
        var options = Instance.ReadJsonSerializerOptions();
        var eventArgs = ParseEventArgs(eventHandlerId, eventName, eventArgsJson, options);
        DispatchEventCore(eventHandlerId, fieldInfo, eventArgs);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "Custom event arg types are preserved by the component that declares the event handler.")]
    private static EventArgs ParseEventArgs(
        long eventHandlerId,
        string eventName,
        string eventArgsJson,
        JsonSerializerOptions options)
    {
        return eventName switch
        {
            "drag" or "dragend" or "dragenter" or "dragleave" or "dragover" or "dragstart" or "drop"
                => JsonSerializer.Deserialize<DragEventArgs>(eventArgsJson, options)!,

            "input" or "change"
                => ParseChangeEventArgs(eventArgsJson, options),

            _ => DeserializeCustomEventArgs(eventHandlerId, eventArgsJson, options),
        };
    }

    internal static DataTransferItem[] UnflattenDataTransferItems(string[]? kinds, string[]? types)
    {
        if (kinds is null || kinds.Length == 0)
        {
            return [];
        }

        var count = kinds.Length;
        var result = new DataTransferItem[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = new DataTransferItem
            {
                Kind = kinds[i],
                Type = types is not null && i < types.Length ? types[i] : string.Empty,
            };
        }

        return result;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "ChangeEventArgs is a well-known type that is preserved.")]
    private static ChangeEventArgs ParseChangeEventArgs(string json, JsonSerializerOptions options)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.Array)
        {
            var length = valueElement.GetArrayLength();
            var values = new string?[length];
            var index = 0;
            foreach (var item in valueElement.EnumerateArray())
            {
                values[index++] = item.GetString();
            }

            return new ChangeEventArgs { Value = values };
        }

        // For non-array values, use standard deserialization
        return JsonSerializer.Deserialize<ChangeEventArgs>(json, options)!;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "Custom event arg types are preserved by the component that declares the event handler.")]
    private static EventArgs DeserializeCustomEventArgs(long eventHandlerId, string eventArgsJson, JsonSerializerOptions options)
    {
        var renderer = Instance.Renderer;
        if (renderer is null)
        {
            return EventArgs.Empty;
        }

        var eventArgsType = renderer.GetEventArgsType((ulong)eventHandlerId);

        return (EventArgs)JsonSerializer.Deserialize(eventArgsJson, eventArgsType, options)!;
    }

    internal static TouchPoint[] UnflattenTouchPoints(double[]? flat)
    {
        if (flat is null || flat.Length == 0)
        {
            return [];
        }

        var count = flat.Length / 7;
        var result = new TouchPoint[count];
        for (var i = 0; i < count; i++)
        {
            var offset = i * 7;
            result[i] = new TouchPoint
            {
                Identifier = (long)flat[offset],
                ScreenX = flat[offset + 1],
                ScreenY = flat[offset + 2],
                ClientX = flat[offset + 3],
                ClientY = flat[offset + 4],
                PageX = flat[offset + 5],
                PageY = flat[offset + 6],
            };
        }

        return result;
    }

    internal static EventFieldInfo? CreateFieldInfo(int fieldComponentId, string? fieldValueString, bool fieldValueBool)
    {
        if (fieldComponentId < 0)
        {
            return null;
        }

        return new EventFieldInfo
        {
            ComponentId = fieldComponentId,
            FieldValue = fieldValueString is not null ? fieldValueString : (fieldValueBool ? BoxedTrue : BoxedFalse),
        };
    }

    private static void DispatchEventCore(long eventHandlerId, EventFieldInfo? fieldInfo, EventArgs eventArgs)
    {
        WebAssemblyCallQueue.Schedule((eventHandlerId, fieldInfo, eventArgs), static state =>
        {
            var renderer = Instance.Renderer;
            if (renderer is not null)
            {
                _ = renderer.DispatchEventAsync((ulong)state.eventHandlerId, state.fieldInfo, state.eventArgs);
            }
        });
    }
}
