import { renderBatch } from '../../Rendering/Renderer';
import { OutOfProcessRenderBatch } from '../../Rendering/RenderBatch/OutOfProcessRenderBatch';
import { ILogger, LogLevel } from '../Logging/ILogger';
import { HubConnection } from '@aspnet/signalr';

export default class RenderQueue {
  private static renderQueues = new Map<number, RenderQueue>();

  private nextBatchId = 2;

  public browserRendererId: number;

  public logger: ILogger;

  public constructor(browserRendererId: number, logger: ILogger) {
    this.browserRendererId = browserRendererId;
    this.logger = logger;
  }

  public static getOrCreateQueue(browserRendererId: number, logger: ILogger): RenderQueue {
    const queue = this.renderQueues.get(browserRendererId);
    if (queue) {
      return queue;
    }

    const newQueue = new RenderQueue(browserRendererId, logger);
    this.renderQueues.set(browserRendererId, newQueue);
    return newQueue;
  }

  public processBatch(receivedBatchId: number, batchData: Uint8Array, connection: HubConnection): void {
    if (receivedBatchId < this.nextBatchId) {
      this.logger.log(LogLevel.Debug, `Batch ${receivedBatchId} already processed. Waiting for batch ${this.nextBatchId}.`);
      return;
    }

    if (receivedBatchId > this.nextBatchId) {
      this.logger.log(LogLevel.Debug, `Waiting for batch ${this.nextBatchId}. Batch ${receivedBatchId} not processed.`);
      return;
    }

    try {
      this.nextBatchId++;
      this.logger.log(LogLevel.Debug, `Applying batch ${receivedBatchId}.`);
      renderBatch(this.browserRendererId, new OutOfProcessRenderBatch(batchData));
      this.completeBatch(connection, receivedBatchId);
    } catch (error) {
      this.logger.log(LogLevel.Error, `There was an error applying batch ${receivedBatchId}.`);

      // If there's a rendering exception, notify server *and* throw on client
      connection.send('OnRenderCompleted', receivedBatchId, error.toString());
      throw error;
    }
  }

  public getLastBatchid(): number {
    return this.nextBatchId - 1;
  }

  private async completeBatch(connection: signalR.HubConnection, batchId: number): Promise<void> {
    try {
      await connection.send('OnRenderCompleted', batchId, null);
    } catch {
      this.logger.log(LogLevel.Warning, `Failed to deliver completion notification for render '${batchId}'.`);
    }
  }
}
