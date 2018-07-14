import '../../Microsoft.JSInterop/JavaScriptRuntime/src/Microsoft.JSInterop';
import './GlobalExports';
import * as Environment from './Environment';
import * as signalR from '@aspnet/signalr';
import { MessagePackHubProtocol } from '@aspnet/signalr-protocol-msgpack';
import { OutOfProcessRenderBatch } from './Rendering/RenderBatch/OutOfProcessRenderBatch';
import { internalFunctions as uriHelperFunctions } from './Services/UriHelper';
import { renderBatch } from './Rendering/Renderer';

let connection : signalR.HubConnection;

function boot() {
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/_blazor')
    .withHubProtocol(new MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();

  connection.on('JS.BeginInvokeJS', DotNet.jsCallDispatcher.beginInvokeJSFromDotNet);
  connection.on('JS.RenderBatch', (browserRendererId: number, batchData: Uint8Array) => {
    renderBatch(browserRendererId, new OutOfProcessRenderBatch(batchData));
  });

  connection.on('JS.Error', unhandledError);

  connection.start()
    .then(() => {
      DotNet.attachDispatcher({
        beginInvokeDotNetFromJS: (callId, assemblyName, methodIdentifier, argsJson) => {
          connection.send('BeginInvokeDotNetFromJS', callId ? callId.toString() : null, assemblyName, methodIdentifier, argsJson);
        }
      });

      connection.send(
        'StartCircuit',
        uriHelperFunctions.getLocationHref(),
        uriHelperFunctions.getBaseURI()
      );
    })
    .catch(unhandledError);
}

function unhandledError(err) {
  console.error(err);

  // Disconnect on errors.
  //
  // TODO: it would be nice to have some kind of experience for what happens when you're
  // trying to interact with an app that's disconnected.
  //
  // Trying to call methods on the connection after its been closed will throw.
  if (connection) {
    connection.stop();
  }
}

boot();
