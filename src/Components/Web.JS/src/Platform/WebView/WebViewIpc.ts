const ipcMessagePrefix = '__bwv:';
const windowExternal = window['external'] as any;

export function sendAttachPage(baseUrl: string, startUrl: string) {
  send(OutgoingMessageType.AttachPage, baseUrl, startUrl);
}

function send(messageType: OutgoingMessageType, ...args: any[]) {
  const serializedMessage = `${ipcMessagePrefix}${JSON.stringify([messageType, ...args])}`;
  windowExternal.sendMessage(serializedMessage);
}

enum OutgoingMessageType {
  AttachPage = 'AttachPage',
  BeginInvokeDotNet = 'BeginInvokeDotNet',
  EndInvokeJS = 'EndInvokeJS',
  DispatchBrowserEvent = 'DispatchBrowserEvent',
  OnRenderCompleted = 'OnRenderCompleted',
  OnLocationChanged = 'OnLocationChanged',
}

enum IncomingMessageType {
  RenderBatch = 'RenderBatch',
  Navigate = 'Navigate',
  AttachToDocument = 'AttachToDocument',
  DetachFromDocument = 'DetachFromDocument',
  EndInvokeDotNet = 'EndInvokeDotNet',
  NotifyUnhandledException = 'NotifyUnhandledException',
}
