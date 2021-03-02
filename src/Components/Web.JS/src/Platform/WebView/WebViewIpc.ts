import { OutOfProcessRenderBatch } from "../../Rendering/RenderBatch/OutOfProcessRenderBatch";
import { attachRootComponentToElement, renderBatch } from "../../Rendering/Renderer";

const ipcMessagePrefix = '__bwv:';
const windowExternal = window['external'] as any;

export function startListener() {
  windowExternal.receiveMessage(onReceivedMessage);
}

export function sendAttachPage(baseUrl: string, startUrl: string) {
  send(OutgoingMessageType.AttachPage, baseUrl, startUrl);
}

export function dispatchBrowserEvent(descriptor: string, eventArgs: string) {
  send(OutgoingMessageType.DispatchBrowserEvent, descriptor, eventArgs);
}

function onReceivedMessage(data: string) {
  if (!data.startsWith(ipcMessagePrefix)) {
    return;
  }

  const parsed = JSON.parse(data.substring(ipcMessagePrefix.length));
  const messageType: IncomingMessageType = parsed[0];
  switch (messageType) {
    case IncomingMessageType.AttachToDocument:
      attachRootComponentToElement(parsed[2], parsed[1]);
      break;
    case IncomingMessageType.RenderBatch:
      const batchId = parsed[1];
      try {
        const batchData = base64ToArrayBuffer(parsed[2]);
        renderBatch(0, new OutOfProcessRenderBatch(batchData));
        send(OutgoingMessageType.OnRenderCompleted, batchId, null);
      } catch (ex) {
        send(OutgoingMessageType.OnRenderCompleted, batchId, ex.toString());
      }
      break;
    default:
      throw new Error(`Unsupported IPC message type '${messageType}'`);
  }

  //
  //    await this.completeBatch(connection, receivedBatchId);
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

// https://stackoverflow.com/a/21797381
// TODO: If the data is large, consider switching over to the native decoder as in https://stackoverflow.com/a/54123275
// But don't force it to be async all the time. Yielding execution leads to perceptible lag.
function base64ToArrayBuffer(base64: string) {
  const binaryString = atob(base64);
  const length = binaryString.length;
  const result = new Uint8Array(length);
  for (let i = 0; i < length; i++) {
      result[i] = binaryString.charCodeAt(i);
  }
  return result;
}
