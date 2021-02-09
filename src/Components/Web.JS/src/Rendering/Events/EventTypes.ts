interface EventTypeOptions {
  browserEventName?: string;
  createEventArgs?: (event: Event) => any;
}

const eventTypeRegistry: Map<string, EventTypeOptions> = new Map();
const emptyEventArgsOptions: EventTypeOptions = {};

export function registerCustomEventType(eventName: string, options: EventTypeOptions): void {
  if (!options) {
    throw new Error('The options parameter is required.');
  }

  // There can't be more than one registration for the same event name because then we wouldn't
  // know which eventargs data to supply.
  if (eventTypeRegistry.has(eventName)) {
    throw new Error(`The event '${eventName}' is already registered.`);
  }

  eventTypeRegistry.set(eventName, options);
}

export function getEventTypeOptions(eventName: string): EventTypeOptions {
  return eventTypeRegistry.get(eventName) || emptyEventArgsOptions;
}

function registerBuiltInEventType(eventNames: string[], options: EventTypeOptions) {
  eventNames.forEach(eventName => eventTypeRegistry.set(eventName, options));
}

registerBuiltInEventType(['input', 'change'], {
  createEventArgs: parseChangeEvent
});

registerBuiltInEventType(['copy', 'cut', 'paste'], emptyEventArgsOptions);

registerBuiltInEventType(['drag', 'dragend', 'dragenter', 'dragleave', 'dragover', 'dragstart', 'drop'], {
  createEventArgs: e => parseDragEvent(e as DragEvent)
});

registerBuiltInEventType(['focus', 'blur', 'focusin', 'focusout'], emptyEventArgsOptions);

registerBuiltInEventType(['keydown', 'keyup', 'keypress'], {
  createEventArgs: e => parseKeyboardEvent(e as KeyboardEvent)
});

registerBuiltInEventType(['contextmenu', 'click', 'mouseover', 'mouseout', 'mousemove', 'mousedown', 'mouseup', 'dblclick'], {
  createEventArgs: e => parseMouseEvent(e as MouseEvent)
});

registerBuiltInEventType(['error'], {
  createEventArgs: e => parseErrorEvent(e as ErrorEvent)
});

registerBuiltInEventType(['loadstart', 'timeout', 'abort', 'load', 'loadend', 'progress'], {
  createEventArgs: e => parseProgressEvent(e as ProgressEvent)
});

registerBuiltInEventType(['touchcancel', 'touchend', 'touchmove', 'touchenter', 'touchleave', 'touchstart'], {
  createEventArgs: e => parseTouchEvent(e as TouchEvent)
});

registerBuiltInEventType(['gotpointercapture', 'lostpointercapture', 'pointercancel', 'pointerdown', 'pointerenter', 'pointerleave', 'pointermove', 'pointerout', 'pointerover', 'pointerup'], {
  createEventArgs: e => parsePointerEvent(e as PointerEvent)
});

registerBuiltInEventType(['wheel', 'mousewheel'], {
  createEventArgs: e => parseWheelEvent(e as WheelEvent)
});

registerBuiltInEventType(['toggle'], emptyEventArgsOptions);

function parseChangeEvent(event: Event): ChangeEventArgs {
  const element = event.target as Element;
  if (isTimeBasedInput(element)) {
    const normalizedValue = normalizeTimeBasedValue(element);
    return { value: normalizedValue };
  } else {
    const targetIsCheckbox = isCheckbox(element);
    const newValue = targetIsCheckbox ? !!element['checked'] : element['value'];
    return { value: newValue };
  }
}

function parseWheelEvent(event: WheelEvent): WheelEventArgs {
  return {
    ...parseMouseEvent(event),
    deltaX: event.deltaX,
    deltaY: event.deltaY,
    deltaZ: event.deltaZ,
    deltaMode: event.deltaMode,
  };
}

function parsePointerEvent(event: PointerEvent): PointerEventArgs {
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

function parseTouchEvent(event: TouchEvent): TouchEventArgs {
  return {
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

function parseProgressEvent(event: ProgressEvent<EventTarget>): ProgressEventArgs {
  return {
    lengthComputable: event.lengthComputable,
    loaded: event.loaded,
    total: event.total,
  };
}

function parseErrorEvent(event: ErrorEvent): ErrorEventArgs {
  return {
    message: event.message,
    filename: event.filename,
    lineno: event.lineno,
    colno: event.colno,
  };
}

function parseKeyboardEvent(event: KeyboardEvent): KeyboardEventArgs {
  return {
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

function parseDragEvent(event: DragEvent): DragEventArgs {
  return {
    ...parseMouseEvent(event),
    dataTransfer: event.dataTransfer ? {
      dropEffect: event.dataTransfer.dropEffect,
      effectAllowed: event.dataTransfer.effectAllowed,
      files: Array.from(event.dataTransfer.files).map(f => f.name),
      items: Array.from(event.dataTransfer.items).map(i => ({ kind: i.kind, type: i.type })),
      types: event.dataTransfer.types,
    } : null,
  };
}

function parseTouch(touchList: TouchList): TouchPoint[] {
  const touches: TouchPoint[] = [];

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

function parseMouseEvent(event: MouseEvent): MouseEventArgs {
  return {
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

// The following interfaces must be kept in sync with the EventArgs C# classes

interface ChangeEventArgs {
  value: string | boolean;
}

interface DragEventArgs {
  detail: number;
  dataTransfer: DataTransferEventArgs | null;
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

interface DataTransferEventArgs {
  dropEffect: string;
  effectAllowed: string;
  files: readonly string[];
  items: readonly DataTransferItem[];
  types: readonly string[];
}

interface DataTransferItem {
  kind: string;
  type: string;
}

interface ErrorEventArgs {
  message: string;
  filename: string;
  lineno: number;
  colno: number;

  // omitting 'error' here since we'd have to serialize it, and it's not clear we will want to
  // do that. https://developer.mozilla.org/en-US/docs/Web/API/ErrorEvent
}

interface KeyboardEventArgs {
  key: string;
  code: string;
  location: number;
  repeat: boolean;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
}

interface MouseEventArgs {
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

interface PointerEventArgs extends MouseEventArgs {
  pointerId: number;
  width: number;
  height: number;
  pressure: number;
  tiltX: number;
  tiltY: number;
  pointerType: string;
  isPrimary: boolean;
}

interface ProgressEventArgs {
  lengthComputable: boolean;
  loaded: number;
  total: number;
}

interface TouchEventArgs {
  detail: number;
  touches: TouchPoint[];
  targetTouches: TouchPoint[];
  changedTouches: TouchPoint[];
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
}

interface TouchPoint {
  identifier: number;
  screenX: number;
  screenY: number;
  clientX: number;
  clientY: number;
  pageX: number;
  pageY: number;
}

interface WheelEventArgs extends MouseEventArgs {
  deltaX: number;
  deltaY: number;
  deltaZ: number;
  deltaMode: number;
}
