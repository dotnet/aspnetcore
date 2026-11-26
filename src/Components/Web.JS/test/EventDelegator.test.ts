// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
  expect, test, describe, beforeEach, afterEach, jest,
} from '@jest/globals';
import { EventDelegator, EventDescriptor } from '../src/Rendering/Events/EventDelegator';
import {
  attachWebRendererInterop,
  detachWebRendererInterop,
  isRendererAttached,
} from '../src/Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../src/Rendering/WebRendererId';
import { tryApplySpecialProperty } from '../src/Rendering/DomSpecialPropertyUtil';
import { registerCustomEventType } from '../src/Rendering/Events/EventTypes';

interface DispatchedCall {
  eventDescriptor: EventDescriptor;
  eventArgs: any;
}

describe('EventDelegator debounce and throttle', () => {
  let dispatchedCalls: DispatchedCall[];
  let container: HTMLElement;

  beforeEach(() => {
    jest.useFakeTimers();
    dispatchedCalls = [];
    container = document.createElement('div');
    document.body.appendChild(container);

    if (isRendererAttached(WebRendererId.Server)) {
      detachWebRendererInterop(WebRendererId.Server);
    }

    const mockInterop: any = {
      invokeMethodAsync: (methodName: string, eventDescriptor: EventDescriptor, eventArgs: any) => {
        if (methodName === 'DispatchEventAsync') {
          dispatchedCalls.push({ eventDescriptor, eventArgs });
        }
        return Promise.resolve();
      },
    };
    attachWebRendererInterop(WebRendererId.Server, mockInterop);
  });

  afterEach(() => {
    jest.useRealTimers();
    if (isRendererAttached(WebRendererId.Server)) {
      detachWebRendererInterop(WebRendererId.Server);
    }
    container.remove();
  });

  test('debounce delays dispatch until silence and sends only the latest input value', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 1, 10);
    delegator.setDebounce(input, 'input', 300);

    input.value = 'a';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(100);

    input.value = 'ab';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(100);

    input.value = 'abc';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(299);

    expect(dispatchedCalls.length).toBe(0);

    jest.advanceTimersByTime(1);
    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventArgs).toEqual({ value: 'abc' });
    expect(dispatchedCalls[0].eventDescriptor.eventFieldInfo?.fieldValue).toBe('abc');

    delegator.removeListenersForElement(input);
  });

  test('throttling click dispatches the first click immediately and drops clicks during the interval', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const button = document.createElement('button');
    container.appendChild(button);

    delegator.setListener(button, 'click', 2, 10);
    delegator.setThrottle(button, 'click', 500);

    button.dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: 10 }));
    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventArgs.clientX).toBe(10);

    jest.advanceTimersByTime(100);
    button.dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: 20 }));
    jest.advanceTimersByTime(200);
    button.dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: 30 }));

    jest.advanceTimersByTime(200);
    expect(dispatchedCalls.length).toBe(1);

    // Cooldown has now elapsed; next click dispatches immediately
    button.dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: 40 }));
    expect(dispatchedCalls.length).toBe(2);
    expect(dispatchedCalls[1].eventArgs.clientX).toBe(40);

    delegator.removeListenersForElement(button);
  });

  test('throttling mousemove dispatches immediately and also dispatches the latest position when the interval ends', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const div = document.createElement('div');
    container.appendChild(div);

    delegator.setListener(div, 'mousemove', 3, 10);
    delegator.setThrottle(div, 'mousemove', 200);

    div.dispatchEvent(new MouseEvent('mousemove', { bubbles: true, clientX: 1 }));
    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventArgs.clientX).toBe(1);

    jest.advanceTimersByTime(50);
    div.dispatchEvent(new MouseEvent('mousemove', { bubbles: true, clientX: 2 }));
    jest.advanceTimersByTime(50);
    div.dispatchEvent(new MouseEvent('mousemove', { bubbles: true, clientX: 3 }));

    expect(dispatchedCalls.length).toBe(1);

    jest.advanceTimersByTime(100);
    expect(dispatchedCalls.length).toBe(2);
    expect(dispatchedCalls[1].eventArgs.clientX).toBe(3);

    delegator.removeListenersForElement(div);
  });

  test('throttling wheel dispatches only the first occurrence of each interval, because wheel events carry deltas', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const div = document.createElement('div');
    container.appendChild(div);

    delegator.setListener(div, 'wheel', 4, 10);
    delegator.setThrottle(div, 'wheel', 200);

    div.dispatchEvent(new WheelEvent('wheel', { bubbles: true, deltaY: 100 }));
    div.dispatchEvent(new WheelEvent('wheel', { bubbles: true, deltaY: 100 }));
    div.dispatchEvent(new WheelEvent('wheel', { bubbles: true, deltaY: 100 }));
    jest.advanceTimersByTime(200);

    expect(dispatchedCalls.length).toBe(1);

    delegator.removeListenersForElement(div);
  });

  test('throttling change dispatches the latest selection when the interval ends and keeps renders from overwriting it meanwhile', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const select = document.createElement('select');
    for (const value of ['a', 'b', 'c', 'd']) {
      const option = document.createElement('option');
      option.value = value;
      select.appendChild(option);
    }
    container.appendChild(select);

    delegator.setListener(select, 'change', 5, 10);
    delegator.setThrottle(select, 'change', 500);

    select.value = 'b';
    select.dispatchEvent(new Event('change', { bubbles: true }));
    select.value = 'c';
    select.dispatchEvent(new Event('change', { bubbles: true }));
    select.value = 'd';
    select.dispatchEvent(new Event('change', { bubbles: true }));
    expect(dispatchedCalls.map(c => c.eventArgs.value)).toEqual(['b']);

    // A render that writes another value while the latest selection is pending doesn't overwrite it
    tryApplySpecialProperty(select, 'value', 'b');
    expect(select.value).toBe('d');

    jest.advanceTimersByTime(500);
    expect(dispatchedCalls.map(c => c.eventArgs.value)).toEqual(['b', 'd']);

    delegator.removeListenersForElement(select);
  });

  test('custom events throttle with a trailing call when registered with throttleTrailing or when aliasing such a browser event', () => {
    registerCustomEventType('testtrailingevent', { throttleTrailing: true });
    registerCustomEventType('testleadingevent', {});
    registerCustomEventType('testscrollalias', { browserEventName: 'scroll' });
    registerCustomEventType('testleadingscrollalias', { browserEventName: 'scroll', throttleTrailing: false });

    const delegator = new EventDelegator(WebRendererId.Server);
    const element = document.createElement('div');
    container.appendChild(element);

    const handlers = [['testtrailingevent', 110], ['testleadingevent', 111], ['testscrollalias', 112], ['testleadingscrollalias', 113]] as const;
    for (const [eventName, eventHandlerId] of handlers) {
      delegator.setListener(element, eventName, eventHandlerId, 1);
      delegator.setThrottle(element, eventName, 200);
    }

    for (const browserEventName of ['testtrailingevent', 'testleadingevent', 'scroll']) {
      element.dispatchEvent(new Event(browserEventName, { bubbles: true }));
      element.dispatchEvent(new Event(browserEventName, { bubbles: true }));
    }
    expect(dispatchedCalls.map(c => c.eventDescriptor.eventHandlerId)).toEqual([110, 111, 112, 113]);

    jest.advanceTimersByTime(200);
    expect(dispatchedCalls.map(c => c.eventDescriptor.eventHandlerId)).toEqual([110, 111, 112, 113, 110, 112]);

    delegator.removeListenersForElement(element);
  });

  test('throttling keydown limits auto-repeat, while each fresh key press gets through and starts a new interval', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'keydown', 6, 1);
    delegator.setThrottle(input, 'keydown', 500);

    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'a' }));
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'a', repeat: true }));
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'a', repeat: true }));
    expect(dispatchedCalls.map(c => c.eventArgs.key)).toEqual(['a']);

    jest.advanceTimersByTime(300);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'b' }));
    jest.advanceTimersByTime(300);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'b', repeat: true }));
    expect(dispatchedCalls.map(c => c.eventArgs.key)).toEqual(['a', 'b']);

    jest.advanceTimersByTime(200);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'b', repeat: true }));
    expect(dispatchedCalls.map(c => c.eventArgs.key)).toEqual(['a', 'b', 'b']);

    delegator.removeListenersForElement(input);
  });

  test('leaving an element or pressing Enter in it dispatches its pending events, while presses and other keys do not', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    const button = document.createElement('button');
    const other = document.createElement('input');
    container.appendChild(input);
    container.appendChild(button);
    container.appendChild(other);

    delegator.setListener(input, 'input', 10, 1);
    delegator.setDebounce(input, 'input', 500);
    delegator.setListener(button, 'mousedown', 11, 1);
    delegator.setListener(button, 'click', 12, 1);
    delegator.setListener(other, 'keydown', 13, 1);
    const dispatchedIds = () => dispatchedCalls.map(c => c.eventDescriptor.eventHandlerId);

    input.value = 'a';
    input.dispatchEvent(new Event('input', { bubbles: true }));

    // Handlers that run before the input loses focus observe the previous value
    button.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
    other.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));
    expect(dispatchedIds()).toEqual([11, 13]);

    // Browsers blur the input when the user presses another element, before the click
    input.dispatchEvent(new FocusEvent('blur'));
    button.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(dispatchedIds()).toEqual([11, 13, 10, 12]);
    expect(dispatchedCalls[2].eventArgs).toEqual({ value: 'a' });

    input.value = 'ab';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));
    expect(dispatchedIds()).toEqual([11, 13, 10, 12, 10]);
    expect(dispatchedCalls[4].eventArgs).toEqual({ value: 'ab' });

    jest.advanceTimersByTime(600);
    expect(dispatchedCalls.length).toBe(5);

    delegator.removeListenersForElement(input);
    delegator.removeListenersForElement(button);
    delegator.removeListenersForElement(other);
  });

  test('a blur raised while a render batch is applied does not dispatch pending events', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 15, 1);
    delegator.setDebounce(input, 'input', 500);

    input.value = 'typed';
    input.dispatchEvent(new Event('input', { bubbles: true }));

    delegator.beginRenderBatch();
    input.dispatchEvent(new FocusEvent('blur'));
    delegator.endRenderBatch();
    expect(dispatchedCalls.length).toBe(0);

    jest.advanceTimersByTime(500);
    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventArgs).toEqual({ value: 'typed' });

    delegator.removeListenersForElement(input);
  });

  test('a click outside the focused element dispatches the pending events of that element and its ancestors, because assistive technologies can click without moving focus', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const field = document.createElement('div');
    const input = document.createElement('input');
    const suggestion = document.createElement('div');
    const canvas = document.createElement('div');
    field.appendChild(input);
    container.appendChild(field);
    container.appendChild(suggestion);
    container.appendChild(canvas);

    delegator.setListener(input, 'input', 20, 1);
    delegator.setDebounce(input, 'input', 500);
    delegator.setListener(field, 'input', 21, 1);
    delegator.setDebounce(field, 'input', 500);
    delegator.setListener(suggestion, 'click', 22, 1);
    delegator.setListener(canvas, 'mousemove', 23, 1);
    delegator.setThrottle(canvas, 'mousemove', 500);
    const dispatchedIds = () => dispatchedCalls.map(c => c.eventDescriptor.eventHandlerId);

    canvas.dispatchEvent(new MouseEvent('mousemove', { bubbles: true, clientX: 1 }));
    canvas.dispatchEvent(new MouseEvent('mousemove', { bubbles: true, clientX: 2 }));
    input.focus();
    input.value = 'a';
    input.dispatchEvent(new Event('input', { bubbles: true }));

    // A click inside the focused element, such as moving the caret, continues typing
    input.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(dispatchedIds()).toEqual([23]);

    // Activating an element that can't take focus leaves focus in the input, so no blur is raised.
    // Pending events of elements outside the focused element's ancestors aren't affected.
    for (const type of ['mousedown', 'mouseup', 'click']) {
      suggestion.dispatchEvent(new MouseEvent(type, { bubbles: true }));
    }
    expect(document.activeElement).toBe(input);
    expect(dispatchedIds()).toEqual([23, 20, 21, 22]);
    expect(dispatchedCalls[1].eventArgs).toEqual({ value: 'a' });

    jest.advanceTimersByTime(600);
    expect(dispatchedIds()).toEqual([23, 20, 21, 22, 23]);
    expect(dispatchedCalls[4].eventArgs.clientX).toBe(2);

    delegator.removeListenersForElement(input);
    delegator.removeListenersForElement(field);
    delegator.removeListenersForElement(suggestion);
    delegator.removeListenersForElement(canvas);
  });

  test('a click outside the focused element follows focus into open shadow trees', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const host = document.createElement('div');
    container.appendChild(host);
    const shadowRoot = host.attachShadow({ mode: 'open' });
    const input = document.createElement('input');
    const suggestion = document.createElement('div');
    shadowRoot.appendChild(input);
    shadowRoot.appendChild(suggestion);

    delegator.setListener(input, 'input', 30, 1);
    delegator.setDebounce(input, 'input', 500);
    delegator.setListener(host, 'input', 31, 1);
    delegator.setDebounce(host, 'input', 500);
    delegator.setListener(suggestion, 'click', 32, 1);

    input.focus();
    input.value = 'a';
    input.dispatchEvent(new Event('input', { bubbles: true, composed: true }));

    input.dispatchEvent(new MouseEvent('click', { bubbles: true, composed: true }));
    expect(dispatchedCalls.length).toBe(0);

    suggestion.dispatchEvent(new MouseEvent('click', { bubbles: true, composed: true }));
    expect(dispatchedCalls.map(c => c.eventDescriptor.eventHandlerId)).toEqual([30, 31, 32]);

    delegator.removeListenersForElement(input);
    delegator.removeListenersForElement(host);
    delegator.removeListenersForElement(suggestion);
  });

  test('flushes pending debounced input on blur even when blur has no .NET handler', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 20, 1);
    delegator.setDebounce(input, 'input', 500);

    input.value = 'typed text';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    expect(dispatchedCalls.length).toBe(0);

    input.dispatchEvent(new FocusEvent('blur', { bubbles: false }));
    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventArgs).toEqual({ value: 'typed text' });

    jest.advanceTimersByTime(600);
    expect(dispatchedCalls.length).toBe(1);

    delegator.removeListenersForElement(input);
  });

  test('live input protection prevents stale render batch value from overwriting input while debounce is pending', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 30, 1);
    delegator.setDebounce(input, 'input', 300);

    input.value = 'user typing';
    input.dispatchEvent(new Event('input', { bubbles: true }));

    // Simulate intermediate render trying to set stale value while debounce is pending
    tryApplySpecialProperty(input, 'value', 'stale server value');
    expect(input.value).toBe('user typing');

    // Once debounce completes, subsequent render updates apply normally
    jest.advanceTimersByTime(300);
    expect(dispatchedCalls.length).toBe(1);

    tryApplySpecialProperty(input, 'value', 'cleared by server');
    expect(input.value).toBe('cleared by server');

    delegator.removeListenersForElement(input);
  });

  test('preventDefault and stopPropagation run synchronously on every event while dispatch is debounced', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const parent = document.createElement('div');
    const child = document.createElement('button');
    parent.appendChild(child);
    container.appendChild(parent);

    delegator.setListener(parent, 'click', 40, 1);
    delegator.setListener(child, 'click', 41, 1);
    delegator.setDebounce(child, 'click', 300);
    delegator.setPreventDefault(child, 'click', true);
    delegator.setStopPropagation(child, 'click', true);

    const evt = new MouseEvent('click', { bubbles: true, cancelable: true });
    child.dispatchEvent(evt);

    expect(evt.defaultPrevented).toBe(true);
    expect(dispatchedCalls.length).toBe(0);

    jest.advanceTimersByTime(300);
    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventDescriptor.eventHandlerId).toBe(41);

    delegator.removeListenersForElement(child);
    delegator.removeListenersForElement(parent);
  });

  test('disposing listener or element cancels pending timer and uses updated eventHandlerId on re-render', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 50, 1);
    delegator.setDebounce(input, 'input', 300);

    input.value = 'updated handler test';
    input.dispatchEvent(new Event('input', { bubbles: true }));

    // Simulate re-render updating the eventHandlerId before the timer fires
    delegator.setListener(input, 'input', 51, 1);
    jest.advanceTimersByTime(300);

    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventDescriptor.eventHandlerId).toBe(51);

    // Now start another debounce and dispose before it fires
    input.value = 'should be cancelled';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    delegator.removeListenersForElement(input);

    jest.advanceTimersByTime(300);
    expect(dispatchedCalls.length).toBe(1);
  });

  test('disabling or switching rate limiting dispatches the pending event instead of dropping it', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 60, 1);
    delegator.setDebounce(input, 'input', 300);

    input.value = 'typed before disable';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    delegator.setDebounce(input, 'input', 0);

    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventArgs).toEqual({ value: 'typed before disable' });

    delegator.setDebounce(input, 'input', 300);
    input.value = 'typed before switch';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    delegator.setThrottle(input, 'input', 300);

    expect(dispatchedCalls.length).toBe(2);
    expect(dispatchedCalls[1].eventArgs).toEqual({ value: 'typed before switch' });

    input.value = 'leading';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.value = 'trailing before disable';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    delegator.setThrottle(input, 'input', 0);

    expect(dispatchedCalls.length).toBe(4);
    expect(dispatchedCalls[3].eventArgs).toEqual({ value: 'trailing before disable' });

    jest.advanceTimersByTime(300);
    expect(dispatchedCalls.length).toBe(4);

    delegator.removeListenersForElement(input);
  });

  test('typing with onkeydown and onkeyup handlers does not prematurely flush oninput:debounce, but Enter key does flush before onkeydown', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 60, 1);
    delegator.setDebounce(input, 'input', 300);
    delegator.setListener(input, 'keydown', 61, 1);
    delegator.setListener(input, 'keyup', 62, 1);

    // Type 'c'
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'c' }));
    input.value = 'c';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(30);
    input.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true, key: 'c' }));

    // Type 'a' 50ms later
    jest.advanceTimersByTime(50);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'a' }));
    input.value = 'ca';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(30);
    input.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true, key: 'a' }));

    // Only keydown and keyup should have been dispatched so far; oninput must still be debouncing!
    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventName === 'input').length).toBe(0);
    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventName === 'keydown').length).toBe(2);
    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventName === 'keyup').length).toBe(2);

    // Now press Enter before the 300ms debounce expires
    jest.advanceTimersByTime(50);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));

    // The pending oninput ('ca') must be flushed BEFORE the Enter keydown is dispatched
    const inputCalls = dispatchedCalls.filter(c => c.eventDescriptor.eventName === 'input');
    expect(inputCalls.length).toBe(1);
    expect(inputCalls[0].eventArgs).toEqual({ value: 'ca' });

    const lastTwo = dispatchedCalls.slice(-2);
    expect(lastTwo[0].eventDescriptor.eventName).toBe('input');
    expect(lastTwo[1].eventDescriptor.eventName).toBe('keydown');
    expect(lastTwo[1].eventArgs.key).toBe('Enter');

    // Advancing past the original 300ms window does not dispatch a duplicate oninput
    jest.advanceTimersByTime(400);
    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventName === 'input').length).toBe(1);

    delegator.removeListenersForElement(input);
  });

  test('parent and child bubbling input handlers do not prematurely flush each other, while moving focus to a sibling input does flush', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const parentDiv = document.createElement('div');
    const childInput = document.createElement('input');
    const siblingInput = document.createElement('input');
    parentDiv.appendChild(childInput);
    container.appendChild(parentDiv);
    container.appendChild(siblingInput);

    // Child is immediate, parent is debounced
    delegator.setListener(childInput, 'input', 70, 1);
    delegator.setListener(parentDiv, 'input', 71, 1);
    delegator.setDebounce(parentDiv, 'input', 300);

    // Sibling is immediate
    delegator.setListener(siblingInput, 'input', 72, 1);

    childInput.value = 'x';
    childInput.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(100);

    childInput.value = 'xy';
    childInput.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(100);

    // Child fired twice; parent must still be debouncing (0 dispatches so far)
    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventHandlerId === 70).length).toBe(2);
    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventHandlerId === 71).length).toBe(0);

    // Moving focus to the sibling blurs the child, which flushes parentDiv's pending input before the sibling's input
    childInput.dispatchEvent(new FocusEvent('blur', { bubbles: false }));
    siblingInput.value = 'other';
    siblingInput.dispatchEvent(new Event('input', { bubbles: true }));

    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventHandlerId === 71).length).toBe(1);
    const lastTwo = dispatchedCalls.slice(-2);
    expect(lastTwo[0].eventDescriptor.eventHandlerId).toBe(71);
    expect(lastTwo[1].eventDescriptor.eventHandlerId).toBe(72);

    delegator.removeListenersForElement(childInput);
    delegator.removeListenersForElement(parentDiv);
    delegator.removeListenersForElement(siblingInput);
  });

  test('keys that are part of an IME composition do not dispatch pending input, while Enter after the composition does', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 100, 1);
    delegator.setDebounce(input, 'input', 500);
    delegator.setListener(input, 'keydown', 101, 1);

    input.value = 'nihon';
    input.dispatchEvent(new Event('input', { bubbles: true }));

    // Enter while composing selects an IME candidate. Browsers also report keyCode 229 for keys the IME handles.
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter', isComposing: true }));
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Process', keyCode: 229 }));
    expect(dispatchedCalls.filter(c => c.eventDescriptor.eventHandlerId === 100).length).toBe(0);

    // Enter after the composition ends dispatches the pending input before the keydown handler runs
    input.value = '日本';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));
    expect(dispatchedCalls.slice(-2).map(c => c.eventDescriptor.eventHandlerId)).toEqual([100, 101]);
    expect(dispatchedCalls[dispatchedCalls.length - 2].eventArgs).toEqual({ value: '日本' });

    delegator.removeListenersForElement(input);
  });

  test('in-flight superseded input values and ABA sequences are handled via monotonic sequence tracking', async () => {
    detachWebRendererInterop(WebRendererId.Server);
    const pendingResolvers: (() => void)[] = [];
    const customInterop: any = {
      invokeMethodAsync: (methodName: string, eventDescriptor: EventDescriptor, eventArgs: any) => {
        if (methodName === 'DispatchEventAsync') {
          dispatchedCalls.push({ eventDescriptor, eventArgs });
        }
        return new Promise<void>(resolve => {
          pendingResolvers.push(resolve);
        });
      },
    };
    attachWebRendererInterop(WebRendererId.Server, customInterop);

    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'input', 90, 1);
    delegator.setDebounce(input, 'input', 100);

    // ABA sequence: user types 'A' (seq 1), then 'AB' (seq 2), then backspaces to 'A' (seq 3), all in flight
    input.value = 'A';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(100);

    input.value = 'AB';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(100);

    input.value = 'A';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    jest.advanceTimersByTime(100);

    expect(dispatchedCalls.length).toBe(3);

    // Server render batch for seq 1 ('A') arrives first, then seq 1 completes
    tryApplySpecialProperty(input, 'value', 'A');
    expect(input.value).toBe('A');
    pendingResolvers[0]();
    await Promise.resolve();

    // Server render batch for seq 2 ('AB') arrives next while seq 3 ('A') is the latest edit: must be suppressed!
    tryApplySpecialProperty(input, 'value', 'AB');
    expect(input.value).toBe('A');
    pendingResolvers[1]();
    await Promise.resolve();

    // Server render batch for seq 3 ('A') completes
    pendingResolvers[2]();
    await Promise.resolve();

    // Once all in-flight dispatches have settled, a subsequent server reset to 'AB' or 'A' applies normally
    tryApplySpecialProperty(input, 'value', 'AB');
    expect(input.value).toBe('AB');
    tryApplySpecialProperty(input, 'value', 'A');
    expect(input.value).toBe('A');

    delegator.removeListenersForElement(input);
  });

  test('disconnected element does not dispatch when its debounce timer expires', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'keydown', 100, 1);
    delegator.setDebounce(input, 'keydown', 300);

    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'z' }));
    input.remove();

    jest.advanceTimersByTime(300);
    expect(dispatchedCalls.length).toBe(0);

    delegator.removeListenersForElement(input);
  });
});
