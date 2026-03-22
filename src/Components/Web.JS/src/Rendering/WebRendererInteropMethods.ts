// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { EventDescriptor } from './Events/EventDelegator';
import { enableJSRootComponents, JSComponentParametersByIdentifier, JSComponentIdentifiersByInitializer } from './JSRootComponents';
import { Blazor } from '../GlobalExports';
import { WebRendererId } from './WebRendererId';

const interopMethodsByRenderer = new Map<number, DotNet.DotNetObject>();
const rendererAttachedListeners: ((browserRendererId: number) => void)[] = [];
const rendererByIdResolverMap: Map<number, [() => void | undefined, Promise<void> | undefined]> = new Map();

export function attachRendererIdResolver(rendererId: number, resolver: () => void | undefined, promise: Promise<void> | undefined) {
  rendererByIdResolverMap.set(rendererId, [resolver, promise]);
}

export function getRendererAttachedPromise(rendererId: number): Promise<void> | undefined {
  return rendererByIdResolverMap.get(rendererId)?.[1];
}

export function attachWebRendererInterop(
  rendererId: number,
  interopMethods: DotNet.DotNetObject,
  jsComponentParameters?: JSComponentParametersByIdentifier,
  jsComponentInitializers?: JSComponentIdentifiersByInitializer,
): void {
  if (interopMethodsByRenderer.has(rendererId)) {
    throw new Error(`Interop methods are already registered for renderer ${rendererId}`);
  }

  interopMethodsByRenderer.set(rendererId, interopMethods);

  if (jsComponentParameters && jsComponentInitializers && Object.keys(jsComponentParameters).length > 0) {
    const manager = getInteropMethods(rendererId);
    enableJSRootComponents(rendererId, manager, jsComponentParameters, jsComponentInitializers);
  }

  rendererByIdResolverMap.get(rendererId)?.[0]?.();

  invokeRendererAttachedListeners(rendererId);
}

export function detachWebRendererInterop(rendererId: number): DotNet.DotNetObject {
  const interopMethods = interopMethodsByRenderer.get(rendererId);
  if (!interopMethods) {
    throw new Error(`Interop methods are not registered for renderer ${rendererId}`);
  }

  interopMethodsByRenderer.delete(rendererId);
  return interopMethods;
}

export function isRendererAttached(browserRendererId: number): boolean {
  return interopMethodsByRenderer.has(browserRendererId);
}

export function registerRendererAttachedListener(listener: (browserRendererId: number) => void) {
  rendererAttachedListeners.push(listener);
}

function invokeRendererAttachedListeners(browserRendererId: number) {
  for (const listener of rendererAttachedListeners) {
    listener(browserRendererId);
  }
}

export function dispatchEvent(browserRendererId: number, eventDescriptor: EventDescriptor, eventArgs: any): void {
  return dispatchEventMiddleware(browserRendererId, eventDescriptor.eventHandlerId, () => {
    const exports = Blazor._internal.dotNetExports;
    if (exports && browserRendererId === WebRendererId.WebAssembly) {
      dispatchEventDirect(exports, eventDescriptor, eventArgs);
      return;
    }
    const interopMethods = getInteropMethods(browserRendererId);
    return interopMethods.invokeMethodAsync('DispatchEventAsync', eventDescriptor, eventArgs);
  });
}

function dispatchEventDirect(
  exports: NonNullable<typeof Blazor._internal.dotNetExports>,
  eventDescriptor: EventDescriptor,
  eventArgs: any,
): void {
  const { eventHandlerId, eventFieldInfo } = eventDescriptor;
  const fieldComponentId = eventFieldInfo ? eventFieldInfo.componentId : -1;
  const fieldValueString = eventFieldInfo && typeof eventFieldInfo.fieldValue === 'string' ? eventFieldInfo.fieldValue : null;
  const fieldValueBool = eventFieldInfo && typeof eventFieldInfo.fieldValue === 'boolean' ? eventFieldInfo.fieldValue : false;

  switch (eventDescriptor.eventName) {
    case 'click': case 'mousedown': case 'mouseup': case 'dblclick':
    case 'contextmenu': case 'mouseover': case 'mouseout': case 'mousemove':
    case 'mouseleave': case 'mouseenter':
      exports.DispatchMouseEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.detail, eventArgs.screenX, eventArgs.screenY,
        eventArgs.clientX, eventArgs.clientY, eventArgs.offsetX, eventArgs.offsetY,
        eventArgs.pageX, eventArgs.pageY, eventArgs.movementX, eventArgs.movementY,
        eventArgs.button, eventArgs.buttons,
        eventArgs.ctrlKey, eventArgs.shiftKey, eventArgs.altKey, eventArgs.metaKey,
        eventArgs.type
      );
      return;

    case 'drag': case 'dragend': case 'dragenter': case 'dragleave':
    case 'dragover': case 'dragstart': case 'drop': {
      const dt = eventArgs.dataTransfer;
      const itemKinds = dt?.items ? dt.items.map((i: any) => i.kind) : null;
      const itemTypes = dt?.items ? dt.items.map((i: any) => i.type) : null;
      exports.DispatchDragEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.detail, eventArgs.screenX, eventArgs.screenY,
        eventArgs.clientX, eventArgs.clientY, eventArgs.offsetX, eventArgs.offsetY,
        eventArgs.pageX, eventArgs.pageY, eventArgs.movementX, eventArgs.movementY,
        eventArgs.button, eventArgs.buttons,
        eventArgs.ctrlKey, eventArgs.shiftKey, eventArgs.altKey, eventArgs.metaKey,
        eventArgs.type,
        dt?.dropEffect ?? null, dt?.effectAllowed ?? null,
        dt?.files ?? null,
        itemKinds, itemTypes,
        dt?.types ?? null
      );
      return;
    }

    case 'keydown': case 'keyup': case 'keypress':
      exports.DispatchKeyboardEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.key, eventArgs.code, eventArgs.location,
        eventArgs.repeat, eventArgs.ctrlKey, eventArgs.shiftKey,
        eventArgs.altKey, eventArgs.metaKey, eventArgs.type, eventArgs.isComposing
      );
      return;

    case 'input': case 'change':
      if (typeof eventArgs.value === 'boolean') {
        exports.DispatchChangeEventBool(eventHandlerId, fieldComponentId, fieldValueBool, eventArgs.value);
        return;
      } else if (typeof eventArgs.value === 'string') {
        exports.DispatchChangeEventString(eventHandlerId, fieldComponentId, fieldValueString, eventArgs.value);
        return;
      } else if (Array.isArray(eventArgs.value)) {
        exports.DispatchChangeEventStringArray(eventHandlerId, fieldComponentId, fieldValueString, eventArgs.value);
        return;
      }
      break;

    case 'focus': case 'blur': case 'focusin': case 'focusout':
      exports.DispatchFocusEvent(eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool, eventArgs.type);
      return;

    case 'copy': case 'cut': case 'paste':
      exports.DispatchClipboardEvent(eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool, eventArgs.type);
      return;

    case 'gotpointercapture': case 'lostpointercapture': case 'pointercancel':
    case 'pointerdown': case 'pointerenter': case 'pointerleave':
    case 'pointermove': case 'pointerout': case 'pointerover': case 'pointerup':
      exports.DispatchPointerEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.detail, eventArgs.screenX, eventArgs.screenY,
        eventArgs.clientX, eventArgs.clientY, eventArgs.offsetX, eventArgs.offsetY,
        eventArgs.pageX, eventArgs.pageY, eventArgs.movementX, eventArgs.movementY,
        eventArgs.button, eventArgs.buttons,
        eventArgs.ctrlKey, eventArgs.shiftKey, eventArgs.altKey, eventArgs.metaKey,
        eventArgs.type,
        eventArgs.pointerId, eventArgs.width, eventArgs.height,
        eventArgs.pressure, eventArgs.tiltX, eventArgs.tiltY,
        eventArgs.pointerType, eventArgs.isPrimary
      );
      return;

    case 'wheel': case 'mousewheel':
      exports.DispatchWheelEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.detail, eventArgs.screenX, eventArgs.screenY,
        eventArgs.clientX, eventArgs.clientY, eventArgs.offsetX, eventArgs.offsetY,
        eventArgs.pageX, eventArgs.pageY, eventArgs.movementX, eventArgs.movementY,
        eventArgs.button, eventArgs.buttons,
        eventArgs.ctrlKey, eventArgs.shiftKey, eventArgs.altKey, eventArgs.metaKey,
        eventArgs.type,
        eventArgs.deltaX, eventArgs.deltaY, eventArgs.deltaZ, eventArgs.deltaMode);
      return;

    case 'touchcancel': case 'touchend': case 'touchmove':
    case 'touchenter': case 'touchleave': case 'touchstart':
      exports.DispatchTouchEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.detail,
        flattenTouchList(eventArgs.touches),
        flattenTouchList(eventArgs.targetTouches),
        flattenTouchList(eventArgs.changedTouches),
        eventArgs.ctrlKey, eventArgs.shiftKey, eventArgs.altKey, eventArgs.metaKey,
        eventArgs.type
      );
      return;

    case 'loadstart': case 'timeout': case 'abort':
    case 'load': case 'loadend': case 'progress':
      exports.DispatchProgressEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.lengthComputable, eventArgs.loaded, eventArgs.total, eventArgs.type
      );
      return;

    case 'error':
      exports.DispatchErrorEvent(
        eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool,
        eventArgs.message, eventArgs.filename, eventArgs.lineno, eventArgs.colno, eventArgs.type
      );
      return;

    case 'cancel': case 'close': case 'submit': case 'toggle':
      exports.DispatchEmptyEvent(eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool);
      return;

    default:
      break; // drag events, custom events → fall through to JSON fallback
  }

  // Fallback: serialize remaining event types as JSON via JSExport
  // Use interop-aware serializer so DotNetObjectReference, Uint8Array, etc. are properly encoded
  let nextByteArrayId = 0;
  const json = JSON.stringify(eventArgs, (_key, value) => {
    if (value instanceof DotNet.DotNetObject) {
      return value.serializeAsArg();
    }
    if (value instanceof Uint8Array) {
      const id = nextByteArrayId++;
      exports.ReceiveByteArrayFromJS(id, value);
      return { ['__byte[]']: id };
    }
    return value;
  });
  exports.DispatchEventJson(eventHandlerId, fieldComponentId, fieldValueString, fieldValueBool, eventDescriptor.eventName, json);
}

function flattenTouchList(touchPoints: any[] | undefined): number[] | null {
  if (!touchPoints || touchPoints.length === 0) {
    return null;
  }
  const count = touchPoints.length;
  const result: number[] = new Array(count * 7);
  for (let i = 0; i < count; i++) {
    const p = touchPoints[i];
    const o = i * 7;
    result[o] = p.identifier;
    result[o + 1] = p.screenX;
    result[o + 2] = p.screenY;
    result[o + 3] = p.clientX;
    result[o + 4] = p.clientY;
    result[o + 5] = p.pageX;
    result[o + 6] = p.pageY;
  }
  return result;
}

export function updateRootComponents(browserRendererId: number, operationsJson: string): void {
  const exports = Blazor._internal.dotNetExports;
  if (exports && browserRendererId === WebRendererId.WebAssembly) {
    exports.UpdateRootComponents(operationsJson, '');
    return;
  }
  const interopMethods = getInteropMethods(browserRendererId);
  interopMethods.invokeMethodAsync('UpdateRootComponents', operationsJson);
}

function getInteropMethods(rendererId: number): DotNet.DotNetObject {
  const interopMethods = interopMethodsByRenderer.get(rendererId);
  if (!interopMethods) {
    throw new Error(`No interop methods are registered for renderer ${rendererId}`);
  }

  return interopMethods;
}

// On some hosting platforms, we may need to defer the event dispatch, so they can register this middleware to do so
type DispatchEventMiddlware = (browserRendererId: number, eventHandlerId: number, continuation: () => void) => void;

let dispatchEventMiddleware: DispatchEventMiddlware = (browserRendererId, eventHandlerId, continuation) => continuation();
export function addDispatchEventMiddleware(middleware: DispatchEventMiddlware): void {
  const next = dispatchEventMiddleware;
  dispatchEventMiddleware = (browserRendererId, eventHandlerId, continuation) => {
    middleware(browserRendererId, eventHandlerId, () => next(browserRendererId, eventHandlerId, continuation));
  };
}
