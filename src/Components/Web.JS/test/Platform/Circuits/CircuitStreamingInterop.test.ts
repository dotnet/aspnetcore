// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection } from '@microsoft/signalr';
import { sendJSDataStream } from '../../../src/Platform/Circuits/CircuitStreamingInterop';

describe('CircuitStreamingInterop', () => {
  afterEach(() => {
    jest.useRealTimers();
  });

  test('acknowledges every chunk', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>().mockResolvedValue(1);
    const send = jest.fn<HubConnection['send']>().mockResolvedValue();
    const onComplete = jest.fn();
    const connection = { invoke, send } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1, 2, 3]), 7, 2, onComplete);
    await jest.runAllTimersAsync();

    expect(invoke).toHaveBeenCalledTimes(2);
    expect(invoke).toHaveBeenNthCalledWith(1, 'ReceiveJSDataChunk', 7, 0, new Uint8Array([1, 2]), null);
    expect(invoke).toHaveBeenNthCalledWith(2, 'ReceiveJSDataChunk', 7, 1, new Uint8Array([3]), null);
    expect(send).not.toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  test('retries a rejected chunk without advancing', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>()
      .mockResolvedValueOnce(2)
      .mockResolvedValueOnce(2)
      .mockResolvedValue(1);
    const connection = { invoke, send: jest.fn<HubConnection['send']>() } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1, 2]), 7, 1);
    await jest.advanceTimersByTimeAsync(0);
    expect(invoke).toHaveBeenCalledTimes(1);

    await jest.advanceTimersByTimeAsync(99);
    expect(invoke).toHaveBeenCalledTimes(1);
    await jest.advanceTimersByTimeAsync(1);
    expect(invoke).toHaveBeenCalledTimes(2);

    await jest.advanceTimersByTimeAsync(199);
    expect(invoke).toHaveBeenCalledTimes(2);
    await jest.advanceTimersByTimeAsync(1);
    expect(invoke).toHaveBeenCalledTimes(4);

    expect(invoke.mock.calls.slice(0, 3).map(call => call.slice(1, 4))).toEqual([
      [7, 0, new Uint8Array([1])],
      [7, 0, new Uint8Array([1])],
      [7, 0, new Uint8Array([1])],
    ]);
    expect(invoke.mock.calls[3].slice(1, 4)).toEqual([7, 1, new Uint8Array([2])]);
  });

  test('stops when the stream is disposed', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>().mockResolvedValue(0);
    const onComplete = jest.fn();
    const connection = { invoke, send: jest.fn<HubConnection['send']>() } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1, 2]), 7, 1, onComplete);
    await jest.runAllTimersAsync();

    expect(invoke).toHaveBeenCalledTimes(1);
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  test('ignores an error reporting failure after the connection closes', async () => {
    jest.useFakeTimers();
    const invoke = jest.fn<HubConnection['invoke']>().mockRejectedValue(new Error('Stream failed.'));
    const send = jest.fn<HubConnection['send']>().mockRejectedValue(new Error("Cannot send data if the connection is not in the 'Connected' State."));
    const onComplete = jest.fn();
    const connection = { invoke, send } as unknown as HubConnection;

    sendJSDataStream(connection, new Uint8Array([1]), 7, 1, onComplete);
    await jest.runAllTimersAsync();

    expect(send).toHaveBeenCalledWith('ReceiveJSDataChunk', 7, -1, null, 'Error: Stream failed.');
    expect(onComplete).toHaveBeenCalledTimes(1);
  });
});