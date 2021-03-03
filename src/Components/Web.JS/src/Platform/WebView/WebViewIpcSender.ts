import { serializeMessage } from './WebViewIpcCommon';

export function sendAttachPage(baseUrl: string, startUrl: string) {
  send('AttachPage', baseUrl, startUrl);
}

export function sendRenderCompleted(batchId: number, errorOrNull: string | null) {
  send('OnRenderCompleted', batchId, errorOrNull);
}

export function dispatchBrowserEvent(descriptor: string, eventArgs: string) {
  send('DispatchBrowserEvent', descriptor, eventArgs);
}

function send(messageType: string, ...args: any[]) {
  const serializedMessage = serializeMessage(messageType, args);
  (window.external as any).sendMessage(serializedMessage);
}
