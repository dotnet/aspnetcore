// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { EventDelegator } from '../src/Rendering/Events/EventDelegator';
import { registerCustomEventType } from '../src/Rendering/Events/EventTypes';
import { dispatchEvent } from '../src/Rendering/WebRendererInteropMethods';

jest.mock('../src/Rendering/WebRendererInteropMethods', () => ({
  isRendererAttached: () => true,
  dispatchEvent: jest.fn(),
}));

const dispatchEventMock = jest.mocked(dispatchEvent);

describe('EventDelegator field tracking metadata', () => {
  let delegator: EventDelegator;
  let host: HTMLElement;
  let input: HTMLInputElement;

  beforeEach(() => {
    dispatchEventMock.mockClear();
    delegator = new EventDelegator(0);
    host = document.createElement('div');
    input = document.createElement('input');
    host.attachShadow({ mode: 'open' }).appendChild(input);
    document.body.appendChild(host);
    input.value = 'field value';
  });

  afterEach(() => {
    delegator.removeListener(1);
    host.remove();
    input.remove();
  });

  test.each(['getter', 'proxy'])('dispatches custom key aliases without inspecting their %s payload', kind => {
    const inspect = jest.fn(() => { throw new Error('Custom payload must not be inspected'); });
    const toJSON = jest.fn(() => ({ key: 'a' }));
    const payload = kind === 'getter'
      ? { get value() { return inspect(); }, toJSON }
      : new Proxy({}, { get: (_, property) => property === 'toJSON' ? toJSON : inspect(), has: inspect });
    const eventName = `custom-key-${kind}`;
    const createEventArgs = jest.fn(() => payload);
    registerCustomEventType(eventName, { browserEventName: 'keydown', createEventArgs });
    delegator.setListener(host, eventName, 1, 42);

    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, composed: true, key: 'a' }));

    expect(createEventArgs).toHaveBeenCalledTimes(1);
    expect(dispatchEventMock).toHaveBeenCalledTimes(1);
    expect(dispatchEventMock.mock.calls[0][1]).toEqual({ eventHandlerId: 1, eventName, eventFieldInfo: null });
    expect(dispatchEventMock.mock.calls[0][2]).toBe(payload);
    expect(toJSON).not.toHaveBeenCalled();
    expect(JSON.stringify(payload)).toBe('{"key":"a"}');
    expect(toJSON).toHaveBeenCalledTimes(1);
    expect(inspect).not.toHaveBeenCalled();
  });

  test('tracks native fields even when custom event args have no field tracking value', () => {
    const payload = { value: 'field value' };
    registerCustomEventType('native-key', { browserEventName: 'keydown', createEventArgs: () => payload });
    delegator.setListener(host, 'native-key', 1, 42);
    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, composed: true }));
    expect(dispatchEventMock).toHaveBeenCalledTimes(1);
    expect(dispatchEventMock.mock.calls[0][1].eventFieldInfo).toBeNull();

    delegator.removeListener(1);
    dispatchEventMock.mockClear();
    host.appendChild(input);
    payload.value = 'not the native value';
    delegator.setListener(input, 'native-key', 1, 42);

    input.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true }));

    expect(dispatchEventMock).toHaveBeenCalledTimes(1);
    expect(dispatchEventMock.mock.calls[0][1].eventFieldInfo).toEqual({ componentId: 42, fieldValue: 'field value' });
    expect(dispatchEventMock.mock.calls[0][2]).toBe(payload);
  });

  test('tracks shadow fields only when built-in parsed scalar values match, without rereading the host', () => {
    let hostValue: unknown = 'field value';
    const readHostValue = jest.fn(() => hostValue);
    Object.defineProperty(host, 'value', { get: readHostValue });
    delegator.setListener(host, 'input', 1, 42);

    function dispatchAndExpectFieldValue(expected: string | boolean | undefined) {
      dispatchEventMock.mockClear();
      readHostValue.mockClear();
      input.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
      expect(dispatchEventMock).toHaveBeenCalledTimes(1);
      expect(dispatchEventMock.mock.calls[0][1].eventFieldInfo).toEqual(
        expected === undefined ? null : { componentId: 42, fieldValue: expected });
      expect(dispatchEventMock.mock.calls[0][2]).toEqual({ value: hostValue });
      expect(readHostValue).toHaveBeenCalledTimes(1);
    }

    dispatchAndExpectFieldValue('field value');
    hostValue = 'different display value';
    dispatchAndExpectFieldValue(undefined);
    hostValue = undefined;
    dispatchAndExpectFieldValue(undefined);
    hostValue = ['field value'];
    dispatchAndExpectFieldValue(undefined);
    input.type = 'checkbox';
    input.checked = false;
    hostValue = false;
    dispatchAndExpectFieldValue(false);
    input.checked = true;
    hostValue = true;
    dispatchAndExpectFieldValue(true);

    delegator.removeListener(1);
    host.appendChild(input);
    delegator.setListener(input, 'change', 1, 42);
    dispatchEventMock.mockClear();
    readHostValue.mockClear();
    input.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
    expect(dispatchEventMock).toHaveBeenCalledTimes(1);
    expect(dispatchEventMock.mock.calls[0][1].eventFieldInfo).toEqual({ componentId: 42, fieldValue: true });
    expect(readHostValue).not.toHaveBeenCalled();
  });
});
