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

  private _renderQueue: RenderQueue;

  private readonly _dispatcher: DotNet.ICallDispatcher;

  private _connection?: HubConnection;

  private _interopMethodsForReconnection?: DotNet.DotNetObject;

  private _circuitId?: string;

  private _startPromise?: Promise<boolean>;

  private _firstUpdate = true;

  private _renderingFailed = false;

  private _disposePromise?: Promise<void>;

  private _disposed = false;

  private _pausingState = new CircuitState<boolean>('pausing', false, false);

  private _resumingState = new CircuitState<boolean>('resuming', false, false);

  private _disconnectingState = new CircuitState<void>('disconnecting');

  private _persistedCircuitState?: { components: string, applicationState: string };

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
      if (handler.onCircuitOpened) {
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

    connection.on('JS.SavePersistedState', (circuitId: string, components: string, applicationState: string) => {
      if (!this._circuitId) {
        throw new Error('Circuit host not initialized.');
      }
      if (circuitId !== this._circuitId) {
        throw new Error(`Received persisted state for circuit ID '${circuitId}', but the current circuit ID is '${this._circuitId}'.`);
      }
      this._persistedCircuitState = { components, applicationState };
      return true;
    });

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

      const pausingWasInProgress = this._pausingState.isInprogress();
      if (!pausingWasInProgress) {
        // Mark the state as 'paused' since the connection got closed without us starting the pause process.
        this._pausingState.transitionTo(true);
      }

      if (!this._disposed && !this._renderingFailed && !pausingWasInProgress) {
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
    if (!this._circuitId) {
      throw new Error('Circuit host not initialized.');
    }

    if (this._disconnectingState.isInprogress()) {
      this._logger.log(LogLevel.Trace, 'Waiting for the circuit to finish disconnecting...');
      return this._disconnectingState.currentProgress();
    }

    try {
      this._disconnectingState.reset();
      const disconnectingPromise = this._disconnectingState.currentProgress();

      this._logger.log(LogLevel.Trace, 'Disconnecting the circuit...');

      await this._connection!.stop();

      this._disconnectingState.complete();
      return disconnectingPromise;
    } catch (error) {
      this._logger.log(LogLevel.Error, `Failed to disconnect the circuit: ${error}`);
      this._disconnectingState.fail(error);
      throw error;
    }
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

  public async pause(remote?: boolean): Promise<boolean> {
    if (!this._circuitId) {
      this._logger.log(LogLevel.Error, 'Circuit host not initialized.');
      return false;
    }

    if (this._connection!.state !== HubConnectionState.Connected) {
      this._logger.log(LogLevel.Trace, 'Pause can only be triggered on connected circuits.');
      return false;
    }

    if (this._resumingState.isInprogress()) {
      this._logger.log(LogLevel.Trace, 'Circuit is currently resuming...');
      return false;
    }

    if (this._pausingState.isInprogress()) {
      this._logger.log(LogLevel.Trace, 'Waiting for the circuit to finish pausing...');
      return this._pausingState.currentProgress();
    }

    if (this._pausingState.lastValue() === true) {
      // If the circuit is already paused, we don't need to do anything.
      this._logger.log(LogLevel.Trace, 'Circuit is already paused.');
      return true;
    }

    this._pausingState.reset();
    const pausingPromise = this._pausingState.currentProgress();

    try {
      this._logger.log(LogLevel.Trace, 'Pausing the circuit...');

      // Notify the reconnection handler that we are pausing the circuit.
      // This is used to trigger the UI display.
      this._options.reconnectionHandler?.onConnectionDown(this._options.reconnectionOptions, undefined, true, remote);

      // Indicate to the server that we want to pause the circuit.
      // The server will initiate the pause on the circuit associated with the current connection.
      const paused = await this._connection!.invoke<boolean>('PauseCircuit')!;
      this._pausingState.complete(paused);
    } catch (error) {
      this._logger.log(LogLevel.Error, `Failed to pause the circuit: ${error}`);
      this._pausingState.fail(error);
    }

    await this.disconnect();

    return pausingPromise;
  }

  public async resume(): Promise<boolean> {
    if (!this._circuitId) {
      this._logger.log(LogLevel.Error, 'Circuit host not initialized.');
      throw new Error('Circuit host not initialized.');
    }

    if (this._disconnectingState.isInprogress()) {
      // The handler might run before the `onclose` handler gets a chance to run.
      this._logger.log(LogLevel.Trace, 'Circuit is disconnecting, cannot resume.');
      await this._disconnectingState.currentProgress();
    }

    if (this._pausingState.isInprogress()) {
      this._logger.log(LogLevel.Trace, 'Waiting for the circuit to finish pausing...');
      return false;
    }

    if (!this._pausingState.lastValue()) {
      // If the circuit is not paused, we cannot resume it.
      this._logger.log(LogLevel.Trace, 'Circuit is not paused.');
      return false;
    }

    if (this._connection!.state !== HubConnectionState.Connected) {
      this._logger.log(LogLevel.Trace, 'Reestablishing SignalR connection...');
      this._connection = await this.startConnection();
    }

    if (this._resumingState.isInprogress()) {
      // If we are already resuming, wait for the current resume operation to complete.
      this._logger.log(LogLevel.Trace, 'Waiting for the circuit to finish resuming...');
      return this._resumingState.currentProgress();
    }

    this._resumingState.reset();
    const resumingPromise = this._resumingState.currentProgress();

    try {
      // When we get here we know the circuit is gone for good.
      // Signal that we are about to start a new circuit so that
      // any existing handlers can perform the necessary cleanup.
      for (const handler of this._options.circuitHandlers) {
        if (handler.onCircuitClosed) {
          handler.onCircuitClosed();
        }
      }

      const persistedCircuitState = this._persistedCircuitState;
      this._persistedCircuitState = undefined;

      const newCircuitId = await this._connection!.invoke<string>(
        'ResumeCircuit',
        this._circuitId,
        navigationManagerFunctions.getBaseURI(),
        navigationManagerFunctions.getLocationHref(),
        persistedCircuitState?.components ?? '[]',
        persistedCircuitState?.applicationState ?? '',
      );
      if (!newCircuitId) {
        this._resumingState.complete(false);
        return resumingPromise;
      }

      this._pausingState.transitionTo(false);
      this._resumingState.complete(true);

      this._circuitId = newCircuitId;
      this._renderQueue = new RenderQueue(this._logger);
      for (const handler of this._options.circuitHandlers) {
        if (handler.onCircuitOpened) {
          handler.onCircuitOpened();
        }
      }

      this._options.reconnectionHandler!.onConnectionUp();
      this._componentManager.onComponentReload?.(WebRendererId.Server);
      return resumingPromise;
    } catch (error) {
      this._logger.log(LogLevel.Error, `Failed to resume the circuit: ${error}`);
      this._resumingState.fail(error);
      return resumingPromise;
    }
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

class CircuitState<T> {

  public constructor(
    private _stateName: string,
    _initialValue?: T,
    private _resetValue?: T
  ) {
    this._lastValue = _initialValue;
  }

  private _promise?: Promise<T>;

  private _resolve?: (value: T | PromiseLike<T>) => void;

  private _reject?: (reason: any) => void;

  private _lastValue?: T;

  public reset(): void {
    if (this._promise) {
      throw new Error(`Circuit state ${this._stateName} is already in progress`);
    }

    const { promise, resolve, reject } = Promise.withResolvers<T>();
    this._promise = promise;
    this._resolve = resolve;
    this._reject = reject;
    this._lastValue = this._resetValue;
  }

  public complete(value: T): void {
    if (!this._resolve) {
      throw new Error(`Circuit state ${this._stateName} not initialized`);
    }

    const resolve = this._resolve;
    this._lastValue = value;

    this._promise = undefined;
    this._resolve = undefined;
    this._reject = undefined;

    resolve(value);
  }

  public fail(reason: any): void {
    if (!this._reject) {
      throw new Error(`Circuit state ${this._stateName} not initialized`);
    }

    const reject = this._reject;

    this._promise = undefined;
    this._resolve = undefined;
    this._reject = undefined;

    reject(reason);
  }

  public isInprogress(): boolean {
    return !!this._promise;
  }

  public currentProgress(): Promise<T> {
    if (!this.isInprogress()) {
      throw new Error(`Circuit state ${this._stateName} is not in progress`);
    }

    return this._promise!;
  }

  public transitionTo(newState: T): void {
    if (this._promise) {
      throw new Error(`Circuit state ${this._stateName} is in progress`);
    }

    this._lastValue = newState;
  }

  public lastValue() {
    return this._lastValue;
  }
}
