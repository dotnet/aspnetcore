import { Logger } from '../Logging/Logger';
import { HubConnection } from '@microsoft/signalr';
export declare class RenderQueue {
    private static instance;
    private nextBatchId;
    private fatalError?;
    browserRendererId: number;
    logger: Logger;
    constructor(browserRendererId: number, logger: Logger);
    static getOrCreate(logger: Logger): RenderQueue;
    processBatch(receivedBatchId: number, batchData: Uint8Array, connection: HubConnection): Promise<void>;
    getLastBatchid(): number;
    private completeBatch;
}
