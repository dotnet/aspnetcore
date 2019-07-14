import '@dotnet/jsinterop';
import './GlobalExports';
import * as signalR from '@aspnet/signalr';
import { MessagePackHubProtocol } from '@aspnet/signalr-protocol-msgpack';
import { shouldAutoStart } from './BootCommon';
import { CircuitHandler } from './Platform/Circuits/CircuitHandler';
import { AutoReconnectCircuitHandler } from './Platform/Circuits/AutoReconnectCircuitHandler';
import RenderQueue from './Platform/Circuits/RenderQueue';
import { ConsoleLogger } from './Platform/Logging/Loggers';
import { LogLevel, ILogger } from './Platform/Logging/ILogger';
import { discoverPrerenderedCircuits, startCircuit } from './Platform/Circuits/CircuitManager';


type SignalRBuilder = (builder: signalR.HubConnectionBuilder) => void;
interface BlazorOptions {
  configureSignalR: SignalRBuilder;
  logLevel: LogLevel;
}

let renderingFailed = false;
let started = false;

async function boot(userOptions?: Partial<BlazorOptions>): Promise<void> {

  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  const defaultOptions: BlazorOptions = {
    configureSignalR: (_) => { },
    logLevel: LogLevel.Warning,
  };

  const options: BlazorOptions = { ...defaultOptions, ...userOptions };

  // For development.
  // Simply put a break point here and modify the log level during
  // development to get traces.
  // In the future we will allow for users to configure this.
  const logger = new ConsoleLogger(options.logLevel);

  logger.log(LogLevel.Information, 'Starting up blazor server-side application.');

  const circuitHandlers: CircuitHandler[] = [new AutoReconnectCircuitHandler(logger)];
  window['Blazor'].circuitHandlers = circuitHandlers;

  // pass options.configureSignalR to configure the signalR.HubConnectionBuilder
  const initialConnection = await initializeConnection(options, circuitHandlers, logger);

  const circuits = discoverPrerenderedCircuits(document);
  for (let i = 0; i < circuits.length; i++) {
    const circuit = circuits[i];
    for (let j = 0; j < circuit.components.length; j++) {
      const component = circuit.components[j];
      component.initialize();
    }
  }

  const circuit = await startCircuit(initialConnection);

  if (!circuit) {
    logger.log(LogLevel.Information, 'No preregistered components to render.');
  }

  const reconnect = async (existingConnection?: signalR.HubConnection): Promise<boolean> => {
    if (renderingFailed) {
      // We can't reconnect after a failure, so exit early.
      return false;
    }
    const reconnection = existingConnection || await initializeConnection(options, circuitHandlers, logger);
    const results = await Promise.all(circuits.map(circuit => circuit.reconnect(reconnection)));

    if (reconnectionFailed(results)) {
      return false;
    }

    circuitHandlers.forEach(h => h.onConnectionUp && h.onConnectionUp());
    return true;
  };

  window['Blazor'].reconnect = reconnect;

  const reconnectTask = reconnect(initialConnection);

  if (circuit) {
    circuits.push(circuit);
  }

  await reconnectTask;

  logger.log(LogLevel.Information, 'Blazor server-side application started.');

  function reconnectionFailed(results: boolean[]): boolean {
    return !results.reduce((current, next) => current && next, true);
  }
}

async function initializeConnection(options: Required<BlazorOptions>, circuitHandlers: CircuitHandler[], logger: ILogger): Promise<signalR.HubConnection> {

  const hubProtocol = new MessagePackHubProtocol();
  (hubProtocol as unknown as { name: string }).name = 'blazorpack';

  const connectionBuilder = new signalR.HubConnectionBuilder()
    .withUrl('_blazor')
    .withHubProtocol(hubProtocol);

  options.configureSignalR(connectionBuilder);

  const connection = connectionBuilder.build();

  connection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  connection.on('JS.RenderBatch', (browserRendererId: number, batchId: number, batchData: Uint8Array) => {
    logger.log(LogLevel.Debug, `Received render batch for ${browserRendererId} with id ${batchId} and ${batchData.byteLength} bytes.`);

    const queue = RenderQueue.getOrCreateQueue(browserRendererId, logger);

    queue.processBatch(batchId, batchData, connection);
  });

  connection.onclose(error => !renderingFailed && circuitHandlers.forEach(h => h.onConnectionDown && h.onConnectionDown(error)));
  connection.on('JS.Error', error => unhandledError(connection, error, logger));

  window['Blazor']._internal.forceCloseConnection = () => connection.stop();

  try {
    await connection.start();
  } catch (ex) {
    unhandledError(connection, ex, logger);
  }

  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson) => {
      connection.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
    },
  });

  return connection;
}

function unhandledError(connection: signalR.HubConnection, err: Error, logger: ILogger): void {
  logger.log(LogLevel.Error, err);

  // Disconnect on errors.
  //
  // Trying to call methods on the connection after its been closed will throw.
  if (connection) {
    renderingFailed = true;
    connection.stop();
  }
}

window['Blazor'].start = boot;
if (shouldAutoStart()) {
  boot();
}
