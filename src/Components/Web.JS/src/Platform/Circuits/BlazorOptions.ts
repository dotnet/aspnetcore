import { LogLevel } from '../Logging/Logger';

export interface BlazorOptions {
  configureSignalR: (builder: signalR.HubConnectionBuilder) => void;
  logLevel: LogLevel;
  reconnectionOptions: ReconnectionOptions;
  reconnectionHandler?: ReconnectionHandler;
}

export function resolveOptions(userOptions?: Partial<BlazorOptions>): BlazorOptions {
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

const defaultOptions: BlazorOptions = {
    configureSignalR: (_) => { },
    logLevel: LogLevel.Warning,
    reconnectionOptions: {
      maxRetries: 5,
      retryIntervalMilliseconds: 3000,
      dialogId: 'components-reconnect-modal',
    },
};
