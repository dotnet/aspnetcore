// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { MessageAckMonitor } from '../../../src/Platform/Circuits/MessageAckMonitor';
import { ConsoleLogger } from '../../../src/Platform/Logging/Loggers';

describe('MessageAckMonitor', () => {
  let connection: HubConnection;
  let logger: ConsoleLogger;

  beforeEach(() => {
    jest.useFakeTimers();
    connection = new HubConnectionBuilder().withUrl('http://localhost/_blazor').build();
    jest.spyOn(connection, 'state', 'get').mockReturnValue(HubConnectionState.Connected);
    jest.spyOn(connection, 'stop').mockResolvedValue();
    logger = new ConsoleLogger(LogLevel.None);
    jest.spyOn(logger, 'log');
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  test('returns the pending acknowledgement and cancels its deadline when acknowledged', async () => {
    const acknowledgement = Promise.withResolvers<void>();
    jest.spyOn(connection, 'invoke').mockReturnValue(acknowledgement.promise);
    const monitor = new MessageAckMonitor(connection, 2000, logger);
    let completed = false;
    const sending = Promise.resolve(monitor.send('OnLocationChanged', '/next', undefined, true))
      .then(() => { completed = true; });

    await Promise.resolve();
    expect(completed).toBe(false);
    acknowledgement.resolve();
    await sending;
    expect(completed).toBe(true);
    jest.advanceTimersByTime(2000);
    expect(connection.stop).not.toHaveBeenCalled();
  });

  test('zero timeout preserves the transport send promise without monitoring', async () => {
    const handoff = Promise.withResolvers<void>();
    jest.spyOn(connection, 'send').mockReturnValue(handoff.promise);
    jest.spyOn(connection, 'invoke');
    const monitor = new MessageAckMonitor(connection, 0, logger);
    let completed = false;
    const sending = Promise.resolve(monitor.send('OnLocationChanging', 1, '/next', undefined, true))
      .then(() => { completed = true; });

    await Promise.resolve();
    jest.advanceTimersByTime(30000);
    expect(completed).toBe(false);
    expect(connection.invoke).not.toHaveBeenCalled();
    expect(connection.stop).not.toHaveBeenCalled();
    handoff.resolve();
    await sending;
    expect(completed).toBe(true);
  });

  test('zero timeout returns a rejected transport send without stopping the connection', async () => {
    const error = new Error('The transport send failed.');
    const handoff = Promise.reject(error);
    jest.spyOn(connection, 'send').mockReturnValue(handoff);
    jest.spyOn(connection, 'invoke');
    const monitor = new MessageAckMonitor(connection, 0, logger);

    const sending = monitor.send('OnLocationChanged', '/next', undefined, true);

    expect(sending).toBe(handoff);
    await expect(sending).rejects.toBe(error);
    expect(connection.send).toHaveBeenCalledWith('OnLocationChanged', '/next', undefined, true);
    expect(connection.invoke).not.toHaveBeenCalled();
    expect(jest.getTimerCount()).toBe(0);
    expect(connection.stop).not.toHaveBeenCalled();
    expect(logger.log).not.toHaveBeenCalled();
  });

  test('a rejected monitored invocation stops the connection once and cancels its deadline', async () => {
    const error = new Error('The invocation failed.');
    const acknowledgement = Promise.reject(error);
    jest.spyOn(connection, 'invoke').mockReturnValue(acknowledgement);
    const monitor = new MessageAckMonitor(connection, 2000, logger);

    const sending = monitor.send('OnLocationChanged', '/next', undefined, true);

    expect(sending).toBe(acknowledgement);
    await expect(sending).rejects.toBe(error);
    expect(connection.stop).toHaveBeenCalledTimes(1);
    expect(logger.log).toHaveBeenCalledWith(LogLevel.Warning, error);
    expect(jest.getTimerCount()).toBe(0);
    jest.advanceTimersByTime(2000);
    expect(connection.stop).toHaveBeenCalledTimes(1);
  });

  test('a later acknowledgement does not extend an earlier message deadline', async () => {
    const first = Promise.withResolvers<void>();
    const second = Promise.withResolvers<void>();
    jest.spyOn(connection, 'invoke')
      .mockReturnValueOnce(first.promise)
      .mockReturnValueOnce(second.promise);
    const monitor = new MessageAckMonitor(connection, 2000, logger);
    monitor.send('BeginInvokeDotNetFromJS');
    jest.advanceTimersByTime(1000);
    monitor.send('OnLocationChanged');
    second.resolve();
    await Promise.resolve();

    jest.advanceTimersByTime(999);
    expect(connection.stop).not.toHaveBeenCalled();
    jest.advanceTimersByTime(1);
    expect(connection.stop).toHaveBeenCalledTimes(1);
    first.reject(new Error('The connection was stopped.'));
    await Promise.resolve();
    jest.advanceTimersByTime(2000);
    expect(connection.stop).toHaveBeenCalledTimes(1);
  });

  test('disposing a connection cancels its deadlines and ignores its late failure', async () => {
    const acknowledgement = Promise.withResolvers<void>();
    jest.spyOn(connection, 'invoke').mockReturnValue(acknowledgement.promise);
    const monitor = new MessageAckMonitor(connection, 2000, logger);
    monitor.send('BeginInvokeDotNetFromJS');

    monitor.dispose();
    acknowledgement.reject(new Error('The old connection was stopped.'));
    await Promise.resolve();
    jest.advanceTimersByTime(2000);

    expect(connection.stop).not.toHaveBeenCalled();
    expect(logger.log).not.toHaveBeenCalled();
  });
});
