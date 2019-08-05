(global as any).DotNet = { attachReviver: jest.fn() };

import { RenderQueue } from '../src/Platform/Circuits/RenderQueue';
import { NullLogger } from '../src/Platform/Logging/Loggers';
import * as signalR from '@aspnet/signalr';

jest.mock('../src/Rendering/Renderer', () => ({
  renderBatch: jest.fn()
}));

describe('RenderQueue', () => {

  it('getOrCreateRenderQueue returns a new queue if one does not exist for a renderer', () => {
    const queue = RenderQueue.getOrCreateQueue(1, NullLogger.instance);

    expect(queue).toBeDefined();

  });

  it('getOrCreateRenderQueue returns an existing queue if one exists for a renderer', () => {
    const queue = RenderQueue.getOrCreateQueue(2, NullLogger.instance);
    const secondQueue = RenderQueue.getOrCreateQueue(2, NullLogger.instance);

    expect(secondQueue).toBe(queue);

  });

  it('processBatch acknowledges previously rendered batches', () => {
    const queue = RenderQueue.getOrCreateQueue(3, NullLogger.instance);

    const sendMock = jest.fn();
    const connection = { send: sendMock } as any as signalR.HubConnection;
    queue.processBatch(2, new Uint8Array(0), connection);

    expect(sendMock.mock.calls.length).toEqual(1);
    expect(queue.getLastBatchid()).toEqual(2);
  });

  it('processBatch does not render out of order batches', () => {
    const queue = RenderQueue.getOrCreateQueue(4, NullLogger.instance);

    const sendMock = jest.fn();
    const connection = { send: sendMock } as any as signalR.HubConnection;
    queue.processBatch(3, new Uint8Array(0), connection);

    expect(sendMock.mock.calls.length).toEqual(0);
  });

  it('processBatch renders pending batches', () => {
    const queue = RenderQueue.getOrCreateQueue(5, NullLogger.instance);

    const sendMock = jest.fn();
    const connection = { send: sendMock } as any as signalR.HubConnection;
    queue.processBatch(2, new Uint8Array(0), connection);

    expect(sendMock.mock.calls.length).toEqual(1);
    expect(queue.getLastBatchid()).toEqual(2);
  });

});
