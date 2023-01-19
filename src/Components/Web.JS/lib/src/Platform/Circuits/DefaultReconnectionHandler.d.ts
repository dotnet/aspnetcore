import { ReconnectionHandler, ReconnectionOptions } from './CircuitStartOptions';
import { ReconnectDisplay } from './ReconnectDisplay';
import { Logger } from '../Logging/Logger';
export declare class DefaultReconnectionHandler implements ReconnectionHandler {
    private readonly _logger;
    private readonly _reconnectCallback;
    private _currentReconnectionProcess;
    private _reconnectionDisplay?;
    constructor(logger: Logger, overrideDisplay?: ReconnectDisplay, reconnectCallback?: () => Promise<boolean>);
    onConnectionDown(options: ReconnectionOptions, _error?: Error): void;
    onConnectionUp(): void;
}
