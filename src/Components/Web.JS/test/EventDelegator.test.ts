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
});
