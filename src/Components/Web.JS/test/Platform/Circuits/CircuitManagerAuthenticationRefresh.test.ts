// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import type { IAuthenticationRefreshOptions } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

interface InternalCircuitManager {
  startConnection(): Promise<HubConnection>;
}

describe('CircuitManager authentication refresh', () => {
  afterEach(() => {
    jest.restoreAllMocks();
    jest.useRealTimers();
  });

  test('enables authentication refresh before applying user configuration', async () => {
    jest.useFakeTimers();
    const configuredOptions: IAuthenticationRefreshOptions[] = [];
    jest.spyOn(HubConnectionBuilder.prototype, 'withAuthenticationRefresh')
      .mockImplementation(function (this: HubConnectionBuilder, options = {}) {
        configuredOptions.push(options);
        return this;
      });

    const connection = {
      on: jest.fn(),
      onclose: jest.fn(),
      start: () => Promise.resolve(),
      state: HubConnectionState.Connected,
    } as unknown as HubConnection;
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);

    const options = resolveOptions({
      configureSignalR: builder => {
        builder.withAuthenticationRefresh({ enableAutoRefresh: false });
      },
    });
    const circuit = new CircuitManager(
      {} as never,
      '',
      options,
      { log: () => { /* no-op */ } } as never,
      new JSEventRegistry());

    await (circuit as unknown as InternalCircuitManager).startConnection();

    expect(configuredOptions).toHaveLength(2);
    expect(configuredOptions[0].onAuthenticationRefreshed).toEqual(expect.any(Function));
    expect(configuredOptions[0].onAuthenticationRefreshFailed).toEqual(expect.any(Function));
    expect(configuredOptions[1]).toEqual({ enableAutoRefresh: false });

    await circuit.dispose();
  });

  test('refreshes authentication every 30 minutes', async () => {
    jest.useFakeTimers();
    let authenticationRefreshOptions: IAuthenticationRefreshOptions | undefined;
    jest.spyOn(HubConnectionBuilder.prototype, 'withAuthenticationRefresh')
      .mockImplementation(function (this: HubConnectionBuilder, options = {}) {
        authenticationRefreshOptions = options;
        return this;
      });

    const refreshAuthentication = jest.fn(() => Promise.resolve(undefined));
    const connection = {
      on: jest.fn(),
      onclose: jest.fn(),
      start: () => Promise.resolve(),
      state: HubConnectionState.Connected,
      refreshAuthentication,
    } as unknown as HubConnection;
    jest.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(connection);

    const circuit = new CircuitManager(
      {} as never,
      '',
      resolveOptions(),
      { log: () => { /* no-op */ } } as never,
      new JSEventRegistry());

    await (circuit as unknown as InternalCircuitManager).startConnection();

    jest.advanceTimersByTime(10 * 60 * 1000);
    await authenticationRefreshOptions!.onAuthenticationRefreshFailed!({ connection } as never);
    jest.advanceTimersByTime(20 * 60 * 1000);
    expect(refreshAuthentication).not.toHaveBeenCalled();

    jest.advanceTimersByTime(10 * 60 * 1000);
    await Promise.resolve();
    await Promise.resolve();

    expect(refreshAuthentication).toHaveBeenCalledTimes(1);
    expect(jest.getTimerCount()).toBe(1);

    jest.advanceTimersByTime(10 * 60 * 1000);
    await authenticationRefreshOptions!.onAuthenticationRefreshed!({ connection } as never);
    jest.advanceTimersByTime(20 * 60 * 1000);
    expect(refreshAuthentication).toHaveBeenCalledTimes(1);

    jest.advanceTimersByTime(10 * 60 * 1000);
    await Promise.resolve();
    await Promise.resolve();

    expect(refreshAuthentication).toHaveBeenCalledTimes(2);
    expect(jest.getTimerCount()).toBe(1);

    await circuit.dispose();
    expect(jest.getTimerCount()).toBe(0);
  });
});
