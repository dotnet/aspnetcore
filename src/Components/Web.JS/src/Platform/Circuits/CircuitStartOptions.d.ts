import { LogLevel } from '../Logging/Logger';
import { HubConnectionBuilder } from '@microsoft/signalr';
export interface CircuitStartOptions {
    configureSignalR: (builder: HubConnectionBuilder) => void;
    logLevel: LogLevel;
    reconnectionOptions: ReconnectionOptions;
    reconnectionHandler?: ReconnectionHandler;
}
export declare function resolveOptions(userOptions?: Partial<CircuitStartOptions>): CircuitStartOptions;
export interface ReconnectionOptions {
    maxRetries: number;
    retryIntervalMilliseconds: number;
    dialogId: string;
}
export interface ReconnectionHandler {
    onConnectionDown(options: ReconnectionOptions, error?: Error): void;
    onConnectionUp(): void;
}
