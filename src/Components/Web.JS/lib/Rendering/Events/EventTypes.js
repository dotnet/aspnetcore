// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
const eventTypeRegistry = new Map();
const browserEventNamesToAliases = new Map();
const createBlankEventArgsOptions = { createEventArgs: () => ({}) };
export const eventNameAliasRegisteredCallbacks = [];
export function registerCustomEventType(eventName, options) {
    if (!options) {
        throw new Error('The options parameter is required.');
    }
    // There can't be more than one registration for the same event name because then we wouldn't
    // know which eventargs data to supply.
    if (eventTypeRegistry.has(eventName)) {
        throw new Error(`The event '${eventName}' is already registered.`);
    }
    // If applicable, register this as an alias of the given browserEventName
    if (options.browserEventName) {
        const aliasGroup = browserEventNamesToAliases.get(options.browserEventName);
        if (aliasGroup) {
            aliasGroup.push(eventName);
        }
        else {
            browserEventNamesToAliases.set(options.browserEventName, [eventName]);
        }
        // For developer convenience, it's allowed to register the custom event type *after*
        // some listeners for it are already present. Once the event name alias gets registered,
        // we have to notify any existing event delegators so they can update their delegated
        // events list.
        eventNameAliasRegisteredCallbacks.forEach(callback => callback(eventName, options.browserEventName));
    }
    eventTypeRegistry.set(eventName, options);
}
export function getEventTypeOptions(eventName) {
    return eventTypeRegistry.get(eventName);
}
export function getEventNameAliases(eventName) {
    return browserEventNamesToAliases.get(eventName);
}
export function getBrowserEventName(possibleAliasEventName) {
    const eventOptions = eventTypeRegistry.get(possibleAliasEventName);
    return (eventOptions === null || eventOptions === void 0 ? void 0 : eventOptions.browserEventName) || possibleAliasEventName;
}
function registerBuiltInEventType(eventNames, options) {
    eventNames.forEach(eventName => eventTypeRegistry.set(eventName, options));
}
registerBuiltInEventType(['input', 'change'], {
    createEventArgs: parseChangeEvent,
});
registerBuiltInEventType([
    'copy',
    'cut',
    'paste',
], {
    createEventArgs: e => parseClipboardEvent(e),
});
registerBuiltInEventType([
    'drag',
    'dragend',
    'dragenter',
    'dragleave',
    'dragover',
    'dragstart',
    'drop',
], {
    createEventArgs: e => parseDragEvent(e),
});
registerBuiltInEventType([
    'focus',
    'blur',
    'focusin',
    'focusout',
], {
    createEventArgs: e => parseFocusEvent(e),
});
registerBuiltInEventType([
    'keydown',
    'keyup',
    'keypress',
], {
    createEventArgs: e => parseKeyboardEvent(e),
});
registerBuiltInEventType([
    'contextmenu',
    'click',
    'mouseover',
    'mouseout',
    'mousemove',
    'mousedown',
    'mouseup',
    'mouseleave',
    'mouseenter',
    'dblclick',
], {
    createEventArgs: e => parseMouseEvent(e),
});
registerBuiltInEventType(['error'], {
    createEventArgs: e => parseErrorEvent(e),
});
registerBuiltInEventType([
    'loadstart',
    'timeout',
    'abort',
    'load',
    'loadend',
    'progress',
], {
    createEventArgs: e => parseProgressEvent(e),
});
registerBuiltInEventType([
    'touchcancel',
    'touchend',
    'touchmove',
    'touchenter',
    'touchleave',
    'touchstart',
], {
    createEventArgs: e => parseTouchEvent(e),
});
registerBuiltInEventType([
    'gotpointercapture',
    'lostpointercapture',
    'pointercancel',
    'pointerdown',
    'pointerenter',
    'pointerleave',
    'pointermove',
    'pointerout',
    'pointerover',
    'pointerup',
], {
    createEventArgs: e => parsePointerEvent(e),
});
registerBuiltInEventType(['wheel', 'mousewheel'], {
    createEventArgs: e => parseWheelEvent(e),
});
registerBuiltInEventType(['toggle'], createBlankEventArgsOptions);
function parseChangeEvent(event) {
    const element = event.target;
    if (isTimeBasedInput(element)) {
        const normalizedValue = normalizeTimeBasedValue(element);
        return { value: normalizedValue };
    }
    else if (isMultipleSelectInput(element)) {
        const selectElement = element;
        const selectedValues = Array.from(selectElement.options)
            .filter(option => option.selected)
            .map(option => option.value);
        return { value: selectedValues };
    }
    else {
        const targetIsCheckbox = isCheckbox(element);
        const newValue = targetIsCheckbox ? !!element['checked'] : element['value'];
        return { value: newValue };
    }
}
function parseWheelEvent(event) {
    return {
        ...parseMouseEvent(event),
        deltaX: event.deltaX,
        deltaY: event.deltaY,
        deltaZ: event.deltaZ,
        deltaMode: event.deltaMode,
    };
}
function parsePointerEvent(event) {
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
function parseTouchEvent(event) {
    return {
        detail: event.detail,
        touches: parseTouch(event.touches),
        targetTouches: parseTouch(event.targetTouches),
        changedTouches: parseTouch(event.changedTouches),
        ctrlKey: event.ctrlKey,
        shiftKey: event.shiftKey,
        altKey: event.altKey,
        metaKey: event.metaKey,
        type: event.type,
    };
}
function parseFocusEvent(event) {
    return {
        type: event.type,
    };
}
function parseClipboardEvent(event) {
    return {
        type: event.type,
    };
}
function parseProgressEvent(event) {
    return {
        lengthComputable: event.lengthComputable,
        loaded: event.loaded,
        total: event.total,
        type: event.type,
    };
}
function parseErrorEvent(event) {
    return {
        message: event.message,
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
        type: event.type,
    };
}
function parseKeyboardEvent(event) {
    return {
        key: event.key,
        code: event.code,
        location: event.location,
        repeat: event.repeat,
        ctrlKey: event.ctrlKey,
        shiftKey: event.shiftKey,
        altKey: event.altKey,
        metaKey: event.metaKey,
        type: event.type,
    };
}
function parseDragEvent(event) {
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
function parseTouch(touchList) {
    const touches = [];
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
function parseMouseEvent(event) {
    return {
        detail: event.detail,
        screenX: event.screenX,
        screenY: event.screenY,
        clientX: event.clientX,
        clientY: event.clientY,
        offsetX: event.offsetX,
        offsetY: event.offsetY,
        pageX: event.pageX,
        pageY: event.pageY,
        movementX: event.movementX,
        movementY: event.movementY,
        button: event.button,
        buttons: event.buttons,
        ctrlKey: event.ctrlKey,
        shiftKey: event.shiftKey,
        altKey: event.altKey,
        metaKey: event.metaKey,
        type: event.type,
    };
}
function isCheckbox(element) {
    return !!element && element.tagName === 'INPUT' && element.getAttribute('type') === 'checkbox';
}
const timeBasedInputs = [
    'date',
    'datetime-local',
    'month',
    'time',
    'week',
];
function isTimeBasedInput(element) {
    return timeBasedInputs.indexOf(element.getAttribute('type')) !== -1;
}
function isMultipleSelectInput(element) {
    return element instanceof HTMLSelectElement && element.type === 'select-multiple';
}
function normalizeTimeBasedValue(element) {
    const value = element.value;
    const type = element.type;
    switch (type) {
        case 'date':
        case 'month':
            return value;
        case 'datetime-local':
            return value.length === 16 ? value + ':00' : value; // Convert yyyy-MM-ddTHH:mm to yyyy-MM-ddTHH:mm:00
        case 'time':
            return value.length === 5 ? value + ':00' : value; // Convert hh:mm to hh:mm:00
        case 'week':
            // For now we are not going to normalize input type week as it is not trivial
            return value;
    }
    throw new Error(`Invalid element type '${type}'.`);
}
//# sourceMappingURL=EventTypes.js.map