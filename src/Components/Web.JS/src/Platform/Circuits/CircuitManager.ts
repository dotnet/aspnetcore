// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { internalFunctions as navigationManagerFunctions } from '../../Services/NavigationManager';
import { toLogicalRootCommentElement, LogicalElement, toLogicalElement } from '../../Rendering/LogicalElements';
import { ServerComponentDescriptor, descriptorToMarker } from '../../Services/ComponentDescriptorDiscovery';
import { HttpTransportType, HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { getAndRemovePendingRootComponentContainer } from '../../Rendering/JSRootComponents';
import { RootComponentManager } from '../../Services/RootComponentManager';
import { CircuitStartOptions } from './CircuitStartOptions';
import { attachRootComponentToLogicalElement } from '../../Rendering/Renderer';
import { WebRendererId } from '../../Rendering/WebRendererId';
import { DotNet } from '@microsoft/dotnet-js-interop';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { ConsoleLogger } from '../Logging/Loggers';
import { RenderQueue } from './RenderQueue';
import { Blazor } from '../../GlobalExports';
import { showErrorNotification } from '../../BootErrors';
import { attachWebRendererInterop, detachWebRendererInterop } from '../../Rendering/WebRendererInteropMethods';
import { sendJSDataStream } from './CircuitStreamingInterop';

export class CircuitManager implements DotNet.DotNetCallDispatcher {
  private readonly _componentManager: RootComponentManager<ServerComponentDescriptor>;

  private _applicationState: string;

  private readonly _options: CircuitStartOptions;

  private readonly _logger: ConsoleLogger;

  private readonly _renderQueue: RenderQueue;

  private readonly _dispatcher: DotNet.ICallDispatcher;

  private _connection?: HubConnection;

  private _interopMethodsForReconnection?: DotNet.DotNetObject;

  private _circuitId?: string;

  private _startPromise?: Promise<boolean>;

  private _firstUpdate = true;

  private _renderingFailed = false;

  private _disposePromise?: Promise<void>;

  private _disposed = false;

  public constructor(
    componentManager: RootComponentManager<ServerComponentDescriptor>,
    appState: string,
    options: CircuitStartOptions,
    logger: ConsoleLogger,
  ) {
    this._circuitId = undefined;
    this._applicationState = appState;
    this._componentManager = componentManager;
    this._options = options;
    this._logger = logger;
    this._renderQueue = new RenderQueue(this._logger);
    this._dispatcher = DotNet.attachDispatcher(this);
  }

  public start(): Promise<boolean> {
    if (this.isDisposedOrDisposing()) {
      throw new Error('Cannot start a disposed circuit.');
    }

    if (!this._startPromise) {
      this._startPromise = this.startCore();
    }

    return this._startPromise;
  }

  public updateRootComponents(operations: string): Promise<void> | undefined {
    if (this._firstUpdate) {
      // Only send the application state on the first update.
      this._firstUpdate = false;
      return this._connection?.send('UpdateRootComponents', operations, this._applicationState);
    } else {
      return this._connection?.send('UpdateRootComponents', operations, '');
    }
  }

  private async startCore(): Promise<boolean> {
    this._connection = await this.startConnection();

    if (this._connection.state !== HubConnectionState.Connected) {
      return false;
    }

    const componentsJson = JSON.stringify(this._componentManager.initialComponents.map(c => descriptorToMarker(c)));
    this._circuitId = await this._connection.invoke<string>(
      'StartCircuit',
      navigationManagerFunctions.getBaseURI(),
      navigationManagerFunctions.getLocationHref(),
      componentsJson,
      this._applicationState || ''
    );

    if (!this._circuitId) {
      return false;
    }

    for (const handler of this._options.circuitHandlers) {
      if (handler.onCircuitOpened){
        handler.onCircuitOpened();
      }
    }

    return true;
  }

  private async startConnection(): Promise<HubConnection> {
    const hubProtocol = new MessagePackHubProtocol();
    (hubProtocol as unknown as { name: string }).name = 'blazorpack';

    const connectionBuilder = new HubConnectionBuilder()
      .withUrl('_blazor')
      .withHubProtocol(hubProtocol);

    this._options.configureSignalR(connectionBuilder);

    const connection = connectionBuilder.build();

    connection.on('JS.AttachComponent', (componentId, selector) => attachRootComponentToLogicalElement(WebRendererId.Server, this.resolveElement(selector), componentId, false));
    connection.on('JS.BeginInvokeJS', this._dispatcher.beginInvokeJSFromDotNet.bind(this._dispatcher));
    connection.on('JS.EndInvokeDotNet', this._dispatcher.endInvokeDotNetFromJS.bind(this._dispatcher));
    connection.on('JS.ReceiveByteArray', this._dispatcher.receiveByteArray.bind(this._dispatcher));

    connection.on('JS.BeginTransmitStream', (streamId: number) => {
      const readableStream = new ReadableStream({
        start: (controller) => {
          connection.stream('SendDotNetStreamToJS', streamId).subscribe({
            next: (chunk: Uint8Array) => controller.enqueue(chunk),
            complete: () => controller.close(),
            error: (err) => controller.error(err),
          });
        },
      });

      this._dispatcher.supplyDotNetStream(streamId, readableStream);
    });

    connection.on('JS.RenderBatch', async (batchId: number, batchData: Uint8Array) => {
      this._logger.log(LogLevel.Debug, `Received render batch with id ${batchId} and ${batchData.byteLength} bytes.`);
      await this._renderQueue.processBatch(batchId, batchData, this._connection!);
      this._componentManager.onAfterRenderBatch?.(WebRendererId.Server);
    });

    connection.on('JS.EndUpdateRootComponents', (batchId: number) => {
      this._componentManager.onAfterUpdateRootComponents?.(batchId);
    });

    connection.on('JS.EndLocationChanging', Blazor._internal.navigationManager.endLocationChanging);
    connection.onclose(error => {
      this._interopMethodsForReconnection = detachWebRendererInterop(WebRendererId.Server);

      if (!this._disposed && !this._renderingFailed) {
        this._options.reconnectionHandler!.onConnectionDown(this._options.reconnectionOptions, error);
      }
    });
    connection.on('JS.Error', error => {
      this._renderingFailed = true;
      this.unhandledError(error);
      showErrorNotification();
    });

    try {
      await connection.start();
    } catch (ex: any) {
      this.unhandledError(ex as Error);

      if (ex.errorType === 'FailedToNegotiateWithServerError') {
        // Connection with the server has been interrupted, and we're in the process of reconnecting.
        // Throw this exception so it can be handled at the reconnection layer, and don't show the
        // error notification.
        throw ex;
      } else {
        showErrorNotification();
      }

      if (ex.innerErrors) {
        if (ex.innerErrors.some(e => e.errorType === 'UnsupportedTransportError' && e.transport === HttpTransportType.WebSockets)) {
          this._logger.log(LogLevel.Error, 'Unable to connect, please ensure you are using an updated browser that supports WebSockets.');
        } else if (ex.innerErrors.some(e => e.errorType === 'FailedToStartTransportError' && e.transport === HttpTransportType.WebSockets)) {
          this._logger.log(LogLevel.Error, 'Unable to connect, please ensure WebSockets are available. A VPN or proxy may be blocking the connection.');
        } else if (ex.innerErrors.some(e => e.errorType === 'DisabledTransportError' && e.transport === HttpTransportType.LongPolling)) {
          this._logger.log(LogLevel.Error, 'Unable to initiate a SignalR connection to the server. This might be because the server is not configured to support WebSockets. For additional details, visit https://aka.ms/blazor-server-websockets-error.');
        }
      }
    }

    // Check if the connection is established using the long polling transport,
    // using the `features.inherentKeepAlive` property only present with long polling.
    if ((connection as any).connection?.features?.inherentKeepAlive) {
      this._logger.log(LogLevel.Warning, 'Failed to connect via WebSockets, using the Long Polling fallback transport. This may be due to a VPN or proxy blocking the connection. To troubleshoot this, visit https://aka.ms/blazor-server-using-fallback-long-polling.');
    }

    return connection;
  }

  public async disconnect(): Promise<void> {
    await this._connection?.stop();
  }

  public async reconnect(): Promise<boolean> {
    if (!this._circuitId) {
      throw new Error('Circuit host not initialized.');
    }

    if (this._connection!.state === HubConnectionState.Connected) {
      return true;
    }

    this._connection = await this.startConnection();

    if (this._interopMethodsForReconnection) {
      attachWebRendererInterop(WebRendererId.Server, this._interopMethodsForReconnection);
      this._interopMethodsForReconnection = undefined;
    }

    if (!await this._connection!.invoke<boolean>('ConnectCircuit', this._circuitId)) {
      return false;
    }

    this._options.reconnectionHandler!.onConnectionUp();

    return true;
  }

  // Implements DotNet.DotNetCallDispatcher
  public beginInvokeDotNetFromJS(callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void {
    this.throwIfDispatchingWhenDisposed();
    this._connection!.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
  }

  // Implements DotNet.DotNetCallDispatcher
  public endInvokeJSFromDotNet(asyncHandle: number, succeeded: boolean, argsJson: any): void {
    this.throwIfDispatchingWhenDisposed();
    this._connection!.send('EndInvokeJSFromDotNet', asyncHandle, succeeded, argsJson);
  }

  // Implements DotNet.DotNetCallDispatcher
  public sendByteArray(id: number, data: Uint8Array): void {
    this.throwIfDispatchingWhenDisposed();
    this._connection!.send('ReceiveByteArray', id, data);
  }

  private throwIfDispatchingWhenDisposed() {
    if (this._disposed) {
      throw new Error('The circuit associated with this dispatcher is no longer available.');
    }
  }

  public sendLocationChanged(uri: string, state: string | undefined, intercepted: boolean): Promise<void> {
    return this._connection!.send('OnLocationChanged', uri, state, intercepted);
  }

  public sendLocationChanging(callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void> {
    return this._connection!.send('OnLocationChanging', callId, uri, state, intercepted);
  }

  public sendJsDataStream(data: ArrayBufferView | Blob, streamId: number, chunkSize: number) {
    return sendJSDataStream(this._connection!, data, streamId, chunkSize);
  }

  public resolveElement(sequenceOrIdentifier: string): LogicalElement {
    // It may be a root component added by JS
    const jsAddedComponentContainer = getAndRemovePendingRootComponentContainer(sequenceOrIdentifier);
    if (jsAddedComponentContainer) {
      return toLogicalElement(jsAddedComponentContainer, true);
    }

    // ... or it may be a root component added by .NET
    const parsedSequence = Number.parseInt(sequenceOrIdentifier);
    if (!Number.isNaN(parsedSequence)) {
      const descriptor = this._componentManager.resolveRootComponent(parsedSequence);
      return toLogicalRootCommentElement(descriptor);
    }

    throw new Error(`Invalid sequence number or identifier '${sequenceOrIdentifier}'.`);
  }

  public getRootComponentManager(): RootComponentManager<ServerComponentDescriptor> {
    return this._componentManager;
  }

  private unhandledError(err: Error): void {
    this._logger.log(LogLevel.Error, err);

    // Disconnect on errors.
    // Trying to call methods on the connection after its been closed will throw.
    this.disconnect();
  }

  private getDisconnectFormData(): FormData {
    const data = new FormData();
    const circuitId = this._circuitId!;
    data.append('circuitId', circuitId);
    return data;
  }

  public didRenderingFail(): boolean {
    return this._renderingFailed;
  }

  public isDisposedOrDisposing(): boolean {
    return this._disposePromise !== undefined;
  }

  public sendDisconnectBeacon() {
    if (this._disposed) {
      return;
    }

    const data = this.getDisconnectFormData();
    this._disposed = navigator.sendBeacon('_blazor/disconnect', data);
  }

  public dispose(): Promise<void> {
    if (!this._disposePromise) {
      this._disposePromise = this.disposeCore();
    }

    return this._disposePromise;
  }

  private async disposeCore(): Promise<void> {
    if (!this._startPromise) {
      // The circuit hasn't started, so there isn't anything to dispose.
      this._disposed = true;
      return;
    }

    // The circuit has started. Wait until it's fully initialized before
    // we start stop the connection.
    await this._startPromise;

    this._disposed = true;
    this._connection?.stop();

    // Dispose the circuit on the server immediately. Closing the SignalR connection alone
    // without sending the disconnect message will result in the circuit being kept
    // alive on the server longer than it needs to be.
    const formData = this.getDisconnectFormData();
    fetch('_blazor/disconnect', {
      method: 'POST',
      body: formData,
    });

    for (const handler of this._options.circuitHandlers) {
      if (handler.onCircuitClosed) {
        handler.onCircuitClosed();
      }
    }
  }
}
