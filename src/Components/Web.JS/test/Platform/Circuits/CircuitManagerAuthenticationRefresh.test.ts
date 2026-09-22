// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, describe, expect, jest, test } from '@jest/globals';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { CircuitManager } from '../../../src/Platform/Circuits/CircuitManager';
import { resolveOptions } from '../../../src/Platform/Circuits/CircuitStartOptions';
import { JSEventRegistry } from '../../../src/Services/JSEventRegistry';

interface InternalCircuitManager {
  startConnection(): Promise<HubConnection>;
}

describe('CircuitManager authentication refresh', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  test('enables authentication refresh before applying user configuration', async () => {
    const configuredOptions: unknown[] = [];
    jest.spyOn(HubConnectionBuilder.prototype, 'withAuthenticationRefresh')
      .mockImplementation(function (this: HubConnectionBuilder, options = {}) {
        configuredOptions.push(options);
        return this;
      });

    const connection = {
      on: jest.fn(),
      onclose: jest.fn(),
      start: () => Promise.resolve(),
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

    expect(configuredOptions).toEqual([{}, { enableAutoRefresh: false }]);
  });
});
