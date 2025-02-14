// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IBlazor } from '../../GlobalExports';
import { LogLevel } from '../Logging/Logger';
import { HubConnectionBuilder } from '@microsoft/signalr';

export type BeforeBlazorServerStartedCallback = (options: Partial<CircuitStartOptions>) => Promise<void>;
export type AfterBlazorServerStartedCallback = (blazor: IBlazor) => Promise<void>;

export type ServerInitializers = {
  beforeStart: BeforeBlazorServerStartedCallback [],
  afterStarted: AfterBlazorServerStartedCallback [],
}

export interface CircuitStartOptions {
  configureSignalR: (builder: HubConnectionBuilder) => void;
  logLevel: LogLevel;
  reconnectionOptions: ReconnectionOptions;
  reconnectionHandler?: ReconnectionHandler;
  initializers : ServerInitializers;
  circuitHandlers: CircuitHandler[];
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
  maxRetries?: number;
  retryIntervalMilliseconds: number | ((previousAttempts: number, maxRetries?: number) => number | undefined | null);
  dialogId: string;
}

export interface CircuitHandler {
  onCircuitOpened?: () => void;
  onCircuitClosed?: () => void;
}

export interface ReconnectionHandler {
  onConnectionDown(options: ReconnectionOptions, error?: Error): void;
  onConnectionUp(): void;
}

function computeDefaultRetryInterval(previousAttempts: number, maxRetries?: number): number | null {
  if (maxRetries && previousAttempts >= maxRetries) {
    return null;
  }

  if (previousAttempts < 10) {
    // Retry as quickly as possible for the first 10 tries
    return 0;
  }

  if (previousAttempts < 20) {
    // Retry every 5 seconds for the next 10 tries
    return 5000;
  }

  // Then retry every 30 seconds indefinitely
  return 30000;
}

const defaultOptions: CircuitStartOptions = {
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  configureSignalR: (_) => { },
  logLevel: LogLevel.Warning,
  initializers: undefined!,
  circuitHandlers: [],
  reconnectionOptions: {
    maxRetries: 30,
    retryIntervalMilliseconds: computeDefaultRetryInterval,
    dialogId: 'components-reconnect-modal',
  },
};
