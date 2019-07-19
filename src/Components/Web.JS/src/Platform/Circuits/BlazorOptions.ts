import { LogLevel } from '../Logging/ILogger';

type SignalRBuilder = (builder: signalR.HubConnectionBuilder) => void;

export interface BlazorOptions {
  configureSignalR: SignalRBuilder;
  logLevel: LogLevel;
}

export function computeCircuitOptions(userOptions?: Partial<BlazorOptions>): BlazorOptions {
    return { ...defaultOptions, ...userOptions };
}

const defaultOptions: BlazorOptions = {
    configureSignalR: (_) => { },
    logLevel: LogLevel.Warning,
};
