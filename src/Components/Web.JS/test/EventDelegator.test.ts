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

  test('discrete throttle (leading-only) dispatches first click immediately and drops clicks during cooldown', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const button = document.createElement('button');
    container.appendChild(button);

    delegator.setListener(button, 'click', 2, 10);
    delegator.setThrottle(button, 'click', 500, false);

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

  test('continuous throttle (leading + trailing) dispatches immediately and emits final trailing state', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const div = document.createElement('div');
    container.appendChild(div);

    delegator.setListener(div, 'mousemove', 3, 10);
    delegator.setThrottle(div, 'mousemove', 200, true);

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

  test('flushes pending debounced input before dispatching un-delayed submit or click event', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const form = document.createElement('form');
    const input = document.createElement('input');
    const submitBtn = document.createElement('button');
    submitBtn.type = 'button';
    form.appendChild(input);
    form.appendChild(submitBtn);
    container.appendChild(form);

    delegator.setListener(input, 'input', 10, 1);
    delegator.setDebounce(input, 'input', 500);
    delegator.setListener(submitBtn, 'click', 11, 1);

    input.value = 'search query';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    expect(dispatchedCalls.length).toBe(0);

    // User immediately clicks submit before 500ms elapses
    submitBtn.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(dispatchedCalls.length).toBe(2);
    expect(dispatchedCalls[0].eventDescriptor.eventName).toBe('input');
    expect(dispatchedCalls[0].eventArgs).toEqual({ value: 'search query' });
    expect(dispatchedCalls[1].eventDescriptor.eventName).toBe('click');

    // Timer must be cancelled so input does not fire again at 500ms
    jest.advanceTimersByTime(600);
    expect(dispatchedCalls.length).toBe(2);

    delegator.removeListenersForElement(input);
    delegator.removeListenersForElement(submitBtn);
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

  test('parent and child bubbling input handlers do not prematurely flush each other, while sibling input does flush', () => {
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

    // Now typing in siblingInput (outside parentDiv's path) flushes parentDiv's pending input before siblingInput's input
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

  test('clicking a link flushes pending debounced input before notifyAfterClick navigation interception runs', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    const link = document.createElement('a');
    link.href = '/other';
    container.appendChild(input);
    container.appendChild(link);

    const eventsOrder: string[] = [];
    delegator.notifyAfterClick(() => {
      eventsOrder.push(`afterClick(dispatched=${dispatchedCalls.length})`);
    });

    delegator.setListener(input, 'input', 80, 1);
    delegator.setDebounce(input, 'input', 500);

    input.value = 'before-nav';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    expect(dispatchedCalls.length).toBe(0);

    link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));

    expect(dispatchedCalls.length).toBe(1);
    expect(dispatchedCalls[0].eventArgs).toEqual({ value: 'before-nav' });
    expect(eventsOrder).toEqual(['afterClick(dispatched=1)']);

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

  test('disconnected element does not dispatch when debounce timer expires, and keyup resets keydown throttle cooldown', () => {
    const delegator = new EventDelegator(WebRendererId.Server);
    const input = document.createElement('input');
    container.appendChild(input);

    delegator.setListener(input, 'keydown', 100, 1);
    delegator.setThrottle(input, 'keydown', 500, false);

    // First keydown dispatches immediately
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'a' }));
    expect(dispatchedCalls.length).toBe(1);

    // Held keydown during cooldown is ignored
    jest.advanceTimersByTime(50);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'a', repeat: true }));
    expect(dispatchedCalls.length).toBe(1);

    // Releasing key (keyup) resets cooldown immediately
    input.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true, key: 'a' }));
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'a' }));
    expect(dispatchedCalls.length).toBe(2);

    // Now test disconnected element with debounce
    delegator.setDebounce(input, 'keydown', 300);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'z' }));
    input.remove(); // Disconnect from DOM before timer fires

    jest.advanceTimersByTime(300);
    expect(dispatchedCalls.length).toBe(2);

    delegator.removeListenersForElement(input);
  });
});
