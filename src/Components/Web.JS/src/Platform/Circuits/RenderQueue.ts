// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { renderBatch } from '../../Rendering/Renderer';
import { OutOfProcessRenderBatch } from '../../Rendering/RenderBatch/OutOfProcessRenderBatch';
import { Logger, LogLevel } from '../Logging/Logger';
import { HubConnection } from '@microsoft/signalr';
import { WebRendererId } from '../../Rendering/WebRendererId';

export class RenderQueue {
  private nextBatchId = 2;

  private fatalError?: string;

  public logger: Logger;

  public constructor(logger: Logger) {
    this.logger = logger;
  }

  public async processBatch(receivedBatchId: number, batchData: Uint8Array, connection: HubConnection): Promise<void> {
    if (receivedBatchId < this.nextBatchId) {
      // SignalR delivers messages in order, but it does not guarantee that the message gets delivered.
      // For that reason, if the server re-sends a batch (for example during a reconnection because it didn't get an ack)
      // we simply acknowledge it to get back in sync with the server.
      await this.completeBatch(connection, receivedBatchId);
      this.logger.log(LogLevel.Debug, `Batch ${receivedBatchId} already processed. Waiting for batch ${this.nextBatchId}.`);
      return;
    }

    if (receivedBatchId > this.nextBatchId) {
      if (this.fatalError) {
        this.logger.log(LogLevel.Debug, `Received a new batch ${receivedBatchId} but errored out on a previous batch ${this.nextBatchId - 1}`);
        await connection.send('OnRenderCompleted', this.nextBatchId - 1, this.fatalError.toString());
        return;
      }

      this.logger.log(LogLevel.Debug, `Waiting for batch ${this.nextBatchId}. Batch ${receivedBatchId} not processed.`);
      return;
    }

    try {
      this.nextBatchId++;
      this.logger.log(LogLevel.Debug, `Applying batch ${receivedBatchId}.`);
      renderBatch(WebRendererId.Server, new OutOfProcessRenderBatch(batchData));
      await this.completeBatch(connection, receivedBatchId);
    } catch (error) {
      this.fatalError = (error as Error).toString();
      this.logger.log(LogLevel.Error, `There was an error applying batch ${receivedBatchId}.`);

      // If there's a rendering exception, notify server *and* throw on client
      connection.send('OnRenderCompleted', receivedBatchId, (error as Error).toString());
      throw error;
    }
  }

  public getLastBatchid(): number {
    return this.nextBatchId - 1;
  }

  private async completeBatch(connection: HubConnection, batchId: number): Promise<void> {
    try {
      await connection.send('OnRenderCompleted', batchId, null);
    } catch {
      this.logger.log(LogLevel.Warning, `Failed to deliver completion notification for render '${batchId}'.`);
    }
  }
}
