export function fromDOMEvent(event: Event): UIEventArgs {
  switch (event.type) {

    case 'input':
    case 'change':
      return parseChangeEvent(event);

    case 'copy':
    case 'cut':
    case 'paste':
      return { type: event.type };

    case 'drag':
    case 'dragend':
    case 'dragenter':
    case 'dragleave':
    case 'dragover':
    case 'dragstart':
    case 'drop':
      return parseDragEvent(event);

    case 'focus':
    case 'blur':
    case 'focusin':
    case 'focusout':
      return { type: event.type };

    case 'keydown':
    case 'keyup':
    case 'keypress':
      return parseKeyboardEvent(event as KeyboardEvent);

    case 'contextmenu':
    case 'click':
    case 'mouseover':
    case 'mouseout':
    case 'mousemove':
    case 'mousedown':
    case 'mouseup':
    case 'dblclick':
      return parseMouseEvent(event as MouseEvent);

    case 'error':
      return parseErrorEvent(event as ErrorEvent);

    case 'loadstart':
    case 'timeout':
    case 'abort':
    case 'load':
    case 'loadend':
    case 'progress':
      return parseProgressEvent(event as ProgressEvent);

    case 'touchcancel':
    case 'touchend':
    case 'touchmove':
    case 'touchenter':
    case 'touchleave':
    case 'touchstart':
      return parseTouchEvent(event as TouchEvent);

    case 'gotpointercapture':
    case 'lostpointercapture':
    case 'pointercancel':
    case 'pointerdown':
    case 'pointerenter':
    case 'pointerleave':
    case 'pointermove':
    case 'pointerout':
    case 'pointerover':
    case 'pointerup':
      return parsePointerEvent(event as PointerEvent);

    case 'wheel':
    case 'mousewheel':
      return parseWheelEvent(event as WheelEvent);

    case 'toggle':
      return { type: event.type };

    default:
      return { type: event.type };
  }
}

function parseChangeEvent(event: any): UIChangeEventArgs {
  const element = event.target as Element;
  if (isTimeBasedInput(element)) {
    const normalizedValue = normalizeTimeBasedValue(element);
    return { type: event.type, value: normalizedValue };
  } else {
    const targetIsCheckbox = isCheckbox(element);
    const newValue = targetIsCheckbox ? !!element['checked'] : element['value'];
    return { type: event.type, value: newValue };
  }
}

function parseDragEvent(event: any): UIDragEventArgs {
  return {
    ...parseMouseEvent(event),
    dataTransfer: event.dataTransfer,
  };
}

function parseWheelEvent(event: WheelEvent): UIWheelEventArgs {
  return {
    ...parseMouseEvent(event),
    deltaX: event.deltaX,
    deltaY: event.deltaY,
    deltaZ: event.deltaZ,
    deltaMode: event.deltaMode,
  };
}

function parseErrorEvent(event: ErrorEvent): UIErrorEventArgs {
  return {
    type: event.type,
    message: event.message,
    filename: event.filename,
    lineno: event.lineno,
    colno: event.colno,
  };
}

function parseProgressEvent(event: ProgressEvent): UIProgressEventArgs {
  return {
    type: event.type,
    lengthComputable: event.lengthComputable,
    loaded: event.loaded,
    total: event.total,
  };
}

function parseTouchEvent(event: TouchEvent): UITouchEventArgs {

  function parseTouch(touchList: TouchList) {
    const touches: UITouchPoint[] = [];

    for (let i = 0; i < touchList.length; i++) {
      const touch = touchList[i];
      touches.push({
        identifier: touch.identifier,
        clientX: touch.clientX,
        clientY: touch.clientY,
        screenX: touch.screenX,
        screenY: touch.screenY,
        pageX: touch.pageX,
        pageY: touch.pageY,
      });
    }
    return touches;
  }

  return {
    type: event.type,
    detail: event.detail,
    touches: parseTouch(event.touches),
    targetTouches: parseTouch(event.targetTouches),
    changedTouches: parseTouch(event.changedTouches),
    ctrlKey: event.ctrlKey,
    shiftKey: event.shiftKey,
    altKey: event.altKey,
    metaKey: event.metaKey,
  };
}

function parseKeyboardEvent(event: KeyboardEvent): UIKeyboardEventArgs {
  return {
    type: event.type,
    key: event.key,
    code: event.code,
    location: event.location,
    repeat: event.repeat,
    ctrlKey: event.ctrlKey,
    shiftKey: event.shiftKey,
    altKey: event.altKey,
    metaKey: event.metaKey,
  };
}

function parsePointerEvent(event: PointerEvent): UIPointerEventArgs {
  return {
    ...parseMouseEvent(event),
    pointerId: event.pointerId,
    width: event.width,
    height: event.height,
    pressure: event.pressure,
    tiltX: event.tiltX,
    tiltY: event.tiltY,
    pointerType: event.pointerType,
    isPrimary: event.isPrimary,
  };
}

function parseMouseEvent(event: MouseEvent): UIMouseEventArgs {
  return {
    type: event.type,
    detail: event.detail,
    screenX: event.screenX,
    screenY: event.screenY,
    clientX: event.clientX,
    clientY: event.clientY,
    offsetX: event.offsetX,
    offsetY: event.offsetY,
    button: event.button,
    buttons: event.buttons,
    ctrlKey: event.ctrlKey,
    shiftKey: event.shiftKey,
    altKey: event.altKey,
    metaKey: event.metaKey,
  };
}

function isCheckbox(element: Element | null): boolean {
  return !!element && element.tagName === 'INPUT' && element.getAttribute('type') === 'checkbox';
}

const timeBasedInputs = [
  'date',
  'datetime-local',
  'month',
  'time',
  'week',
];

function isTimeBasedInput(element: Element): element is HTMLInputElement {
  return timeBasedInputs.indexOf(element.getAttribute('type')!) !== -1;
}

function normalizeTimeBasedValue(element: HTMLInputElement): string {
  const value = element.value;
  const type = element.type;
  switch (type) {
    case 'date':
    case 'datetime-local':
    case 'month':
      return value;
    case 'time':
      return value.length === 5 ? value + ':00' : value; // Convert hh:mm to hh:mm:00
    case 'week':
      // For now we are not going to normalize input type week as it is not trivial
      return value;
  }

  throw new Error(`Invalid element type '${type}'.`);
}

// The following interfaces must be kept in sync with the UIEventArgs C# classes

export interface UIEventArgs {
  type: string;
}

interface UIChangeEventArgs extends UIEventArgs {
  value: string | boolean;
}

interface UIClipboardEventArgs extends UIEventArgs {
}

interface UIDragEventArgs extends UIEventArgs {
  detail: number;
  dataTransfer: UIDataTransfer;
  screenX: number;
  screenY: number;
  clientX: number;
  clientY: number;
  button: number;
  buttons: number;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
}

interface UIDataTransfer {
  dropEffect: string;
  effectAllowed: string;
  files: string[];
  items: UIDataTransferItem[];
  types: string[];
}

interface UIDataTransferItem {
  kind: string;
  type: string;
}

interface UIErrorEventArgs extends UIEventArgs {
  message: string;
  filename: string;
  lineno: number;
  colno: number;

  // omitting 'error' here since we'd have to serialize it, and it's not clear we will want to
  // do that. https://developer.mozilla.org/en-US/docs/Web/API/ErrorEvent
}

interface UIFocusEventArgs extends UIEventArgs {
}

interface UIKeyboardEventArgs extends UIEventArgs {
  key: string;
  code: string;
  location: number;
  repeat: boolean;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
}

interface UIMouseEventArgs extends UIEventArgs {
  detail: number;
  screenX: number;
  screenY: number;
  clientX: number;
  clientY: number;
  offsetX: number;
  offsetY: number;
  button: number;
  buttons: number;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
}

interface UIPointerEventArgs extends UIMouseEventArgs {
  pointerId: number;
  width: number;
  height: number;
  pressure: number;
  tiltX: number;
  tiltY: number;
  pointerType: string;
  isPrimary: boolean;
}

interface UIProgressEventArgs extends UIEventArgs {
  lengthComputable: boolean;
  loaded: number;
  total: number;
}

interface UITouchEventArgs extends UIEventArgs {
  detail: number;
  touches: UITouchPoint[];
  targetTouches: UITouchPoint[];
  changedTouches: UITouchPoint[];
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
}

interface UITouchPoint {
  identifier: number;
  screenX: number;
  screenY: number;
  clientX: number;
  clientY: number;
  pageX: number;
  pageY: number;
}

interface UIWheelEventArgs extends UIMouseEventArgs {
  deltaX: number;
  deltaY: number;
  deltaZ: number;
  deltaMode: number;
}
