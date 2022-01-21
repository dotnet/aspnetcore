// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { LogLevel } from '../Logging/Logger';
import { HubConnectionBuilder } from '@microsoft/signalr';

export interface CircuitStartOptions {
  configureSignalR: (builder: HubConnectionBuilder) => void;
  logLevel: LogLevel;
  reconnectionOptions: ReconnectionOptions;
  reconnectionHandler?: ReconnectionHandler;
}

export function resolveOptions(userOptions?: Partial<CircuitStartOptions>): CircuitStartOptions {
  const result = { ...defaultOptions, ...userOptions };

  // The spread operator can't be used for a deep merge, so do the same for subproperties
  if (userOptions && userOptions.reconnectionOptions) {
    result.reconnectionOptions = { ...defaultOptions.reconnectionOptions, ...userOptions.reconnectionOptions };
  }

  return result;
}

export interface ReconnectionOptions {
  maxRetries: number;
  retryIntervalMilliseconds: number;
  dialogId: string;
}

export interface ReconnectionHandler {
  onConnectionDown(options: ReconnectionOptions, error?: Error): void;
  onConnectionUp(): void;
}

const defaultOptions: CircuitStartOptions = {
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  configureSignalR: (_) => { },
  logLevel: LogLevel.Warning,
  reconnectionOptions: {
    maxRetries: 8,
    retryIntervalMilliseconds: 20000,
    dialogId: 'components-reconnect-modal',
  },
};
