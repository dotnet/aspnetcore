import { EventDescriptor } from '../../Rendering/Events/EventDispatcher';
import { trySerializeMessage } from './WebViewIpcCommon';

export function sendAttachPage(baseUrl: string, startUrl: string) {
  send('AttachPage', baseUrl, startUrl);
}

export function sendRenderCompleted(batchId: number, errorOrNull: string | null) {
  send('OnRenderCompleted', batchId, errorOrNull);
}

export function sendBrowserEvent(descriptor: EventDescriptor, eventArgs: any) {
  send('DispatchBrowserEvent', descriptor, eventArgs);
}

export function sendBeginInvokeDotNetFromJS(callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void {
  send('BeginInvokeDotNet', callId ? callId.toString() : null, assemblyName, methodIdentifier, dotNetObjectId || 0, argsJson);
}

export function sendEndInvokeJSFromDotNet(asyncHandle: number, succeeded: boolean, argsJson: any) {
  send('EndInvokeJS', asyncHandle, succeeded, argsJson);
}

export function sendLocationChanged(uri: string, intercepted: boolean) {
  send('OnLocationChanged', uri, intercepted);
  return Promise.resolve(); // Like in Blazor Server, we only issue the notification here - there's no need to wait for a response
}

function send(messageType: string, ...args: any[]) {
  const serializedMessage = trySerializeMessage(messageType, args);
  if (serializedMessage) {
    (window.external as any).sendMessage(serializedMessage);
  }
}
