// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { sendJSDataStream } from '../../../src/Platform/Circuits/CircuitStreamingInterop';

describe('CircuitStreamingInterop', () => {
  afterEach(() => {
    jest.restoreAllMocks();
    jest.useRealTimers();
  });

  test('starts by pipelining five chunks', async () => {
    jest.useFakeTimers();
    let activeInvocations = 0;
    let maximumActiveInvocations = 0;
    const invoke = jest.fn(async () => {
      activeInvocations++;
      maximumActiveInvocations = Math.max(maximumActiveInvocations, activeInvocations);
      await new Promise(resolve => setTimeout(resolve, 10));
      activeInvocations--;
      return 1;
    });
    const send = jest.fn<HubConnection['send']>().mockResolvedValue();
    const onComplete = jest.fn();
    const connection = { invoke, send } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1, 2, 3, 4, 5, 6]), 7, 1, 1000, onComplete);
    await jest.runAllTimersAsync();

    expect(invoke).toHaveBeenCalledTimes(6);
    expect(maximumActiveInvocations).toBe(5);
    expect(invoke).toHaveBeenNthCalledWith(1, 'ReceiveJSDataChunk', 7, 0, new Uint8Array([1]), null);
    expect(invoke).toHaveBeenNthCalledWith(6, 'ReceiveJSDataChunk', 7, 5, new Uint8Array([6]), null);
    expect(send).not.toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  test('grows the send window on accepted batches and bounds chunks in flight', async () => {
    jest.useFakeTimers();
    let activeInvocations = 0;
    let maximumActiveInvocations = 0;
    const invoke = jest.fn(async () => {
      activeInvocations++;
      maximumActiveInvocations = Math.max(maximumActiveInvocations, activeInvocations);
      await new Promise(resolve => setTimeout(resolve, 10));
      activeInvocations--;
      return 1;
    });
    const send = jest.fn<HubConnection['send']>();
    const onComplete = jest.fn();
    const connection = { invoke, send } as unknown as HubConnection;
    const data = Uint8Array.from({ length: 600 }, (_, index) => index % 256);

    sendJSDataStream(connection, data, 7, 1, 1000, onComplete);
    await jest.advanceTimersByTimeAsync(0);
    expect(invoke).toHaveBeenCalledTimes(5);

    await jest.advanceTimersByTimeAsync(10);
    expect(invoke).toHaveBeenCalledTimes(15);
    await jest.advanceTimersByTimeAsync(10);
    expect(invoke).toHaveBeenCalledTimes(35);

    await jest.advanceTimersByTimeAsync(79);
    expect(onComplete).not.toHaveBeenCalled();
    await jest.advanceTimersByTimeAsync(1);

    expect(invoke).toHaveBeenCalledTimes(data.length);
    expect(maximumActiveInvocations).toBe(100);
    for (let index = 0; index < data.length; index++) {
      expect(invoke).toHaveBeenNthCalledWith(index + 1, 'ReceiveJSDataChunk', 7, index, data.slice(index, index + 1), null);
    }
    expect(send).not.toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  test('shrinks a grown window on backpressure and retries in order before growing again', async () => {
    jest.useFakeTimers();
    let invocationCount = 0;
    const invoke = jest.fn(async (..._args: Parameters<HubConnection['invoke']>) => {
      const callIndex = invocationCount++;
      await new Promise(resolve => setTimeout(resolve, 10));
      return callIndex >= 6 && callIndex < 20 ? 2 : 1;
    });
    const send = jest.fn<HubConnection['send']>();
    const onComplete = jest.fn();
    const connection = { invoke, send } as unknown as HubConnection;
    const data = Uint8Array.from({ length: 30 }, (_, index) => index);

    sendJSDataStream(connection, data, 7, 1, 1000, onComplete);
    await jest.advanceTimersByTimeAsync(20);
    expect(invoke).toHaveBeenCalledTimes(15);

    await jest.advanceTimersByTimeAsync(99);
    expect(invoke).toHaveBeenCalledTimes(15);
    await jest.advanceTimersByTimeAsync(1);
    expect(invoke).toHaveBeenCalledTimes(20);
    expect(invoke.mock.calls.slice(15).map(call => call[2])).toEqual([
      6,
      7,
      8,
      9,
      10,
    ]);

    await jest.advanceTimersByTimeAsync(110);
    expect(invoke).toHaveBeenCalledTimes(22);
    expect(invoke.mock.calls.slice(20).map(call => call[2])).toEqual([6, 7]);
    await jest.advanceTimersByTimeAsync(10);
    expect(invoke).toHaveBeenCalledTimes(26);
    expect(invoke.mock.calls.slice(22).map(call => call[2])).toEqual([
      8,
      9,
      10,
      11,
    ]);

    await jest.runAllTimersAsync();

    expect(invoke.mock.calls.slice(20).map(call => call[2])).toEqual(Array.from({ length: 24 }, (_, index) => index + 6));
    for (const call of invoke.mock.calls) {
      expect(call[3]).toEqual(data.slice(call[2], call[2] + 1));
    }
    expect(send).not.toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  test('advances accepted chunks and retries the rejected suffix', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>()
      .mockResolvedValueOnce(1)
      .mockResolvedValueOnce(2)
      .mockResolvedValueOnce(1);
    const connection = { invoke, send: jest.fn<HubConnection['send']>() } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1, 2]), 7, 1, 1000);
    await jest.advanceTimersByTimeAsync(0);
    expect(invoke).toHaveBeenCalledTimes(2);

    await jest.advanceTimersByTimeAsync(99);
    expect(invoke).toHaveBeenCalledTimes(2);
    await jest.advanceTimersByTimeAsync(1);
    expect(invoke).toHaveBeenCalledTimes(3);

    expect(invoke.mock.calls.map(call => call.slice(1, 4))).toEqual([
      [7, 0, new Uint8Array([1])],
      [7, 1, new Uint8Array([2])],
      [7, 1, new Uint8Array([2])],
    ]);
  });

  test('bounds retry delay by the server timeout', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>()
      .mockResolvedValueOnce(2)
      .mockResolvedValueOnce(1);
    const connection = { invoke, send: jest.fn<HubConnection['send']>() } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1]), 7, 1, 200);
    await jest.advanceTimersByTimeAsync(0);
    expect(invoke).toHaveBeenCalledTimes(1);

    await jest.advanceTimersByTimeAsync(49);
    expect(invoke).toHaveBeenCalledTimes(1);
    await jest.advanceTimersByTimeAsync(1);
    expect(invoke).toHaveBeenCalledTimes(2);
  });

  test('caps exponential retry delays during sustained backpressure', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>().mockResolvedValue(2);
    const send = jest.fn<HubConnection['send']>();
    const onComplete = jest.fn();
    const connection = { invoke, send } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1]), 7, 1, 500, onComplete);
    await jest.advanceTimersByTimeAsync(0);
    expect(invoke).toHaveBeenCalledTimes(1);

    let attempts = 1;
    for (const delay of [100, 125, 125, 125, 125]) {
      await jest.advanceTimersByTimeAsync(delay - 1);
      expect(invoke).toHaveBeenCalledTimes(attempts);
      await jest.advanceTimersByTimeAsync(1);
      expect(invoke).toHaveBeenCalledTimes(++attempts);
    }

    invoke.mockResolvedValue(1);
    await jest.advanceTimersByTimeAsync(125);

    expect(invoke).toHaveBeenCalledTimes(attempts + 1);
    for (const call of invoke.mock.calls) {
      expect(call).toEqual(['ReceiveJSDataChunk', 7, 0, new Uint8Array([1]), null]);
    }
    expect(send).not.toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  test('stops when the stream is disposed', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>().mockResolvedValue(0);
    const onComplete = jest.fn();
    const connection = { invoke, send: jest.fn<HubConnection['send']>() } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1, 2]), 7, 1, 1000, onComplete);
    await jest.runAllTimersAsync();

    expect(invoke).toHaveBeenCalledTimes(2);
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  test.each([
    HubConnectionState.Connected,
    HubConnectionState.Disconnected,
    HubConnectionState.Reconnecting,
  ])('surfaces an error reporting failure when the connection is %s', async (state) => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>().mockRejectedValue(new Error('Stream failed.'));
    const sendError = new Error('Send failed.');
    const send = jest.fn<HubConnection['send']>().mockRejectedValue(sendError);
    const onComplete = jest.fn();
    const connection = { invoke, send, state } as unknown as HubConnection;
    const setTimeoutSpy = jest.spyOn(globalThis, 'setTimeout');

    sendJSDataStream(connection, new Uint8Array([1]), 7, 1, 1000, onComplete);
    const backgroundWork = setTimeoutSpy.mock.calls[0][0] as () => Promise<void>;

    await expect(backgroundWork()).rejects.toBe(sendError);
    expect(send).toHaveBeenCalledWith('ReceiveJSDataChunk', 7, -1, null, 'Error: Stream failed.');
    expect(onComplete).toHaveBeenCalledTimes(1);
  });
});