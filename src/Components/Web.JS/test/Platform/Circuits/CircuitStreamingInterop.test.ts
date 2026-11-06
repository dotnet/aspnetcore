// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection } from '@microsoft/signalr';
import { sendJSDataStream } from '../../../src/Platform/Circuits/CircuitStreamingInterop';

describe('CircuitStreamingInterop', () => {
  afterEach(() => {
    jest.useRealTimers();
  });

  test('pipelines up to five chunks', async () => {
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

  test('ignores an error reporting failure after the connection closes', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>().mockRejectedValue(new Error('Stream failed.'));
    const send = jest.fn<HubConnection['send']>().mockRejectedValue(new Error("Cannot send data if the connection is not in the 'Connected' State."));
    const onComplete = jest.fn();
    const connection = { invoke, send } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1]), 7, 1, 1000, onComplete);
    await jest.runAllTimersAsync();

    expect(send).toHaveBeenCalledWith('ReceiveJSDataChunk', 7, -1, null, 'Error: Stream failed.');
    expect(onComplete).toHaveBeenCalledTimes(1);
  });
});