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

async function onReceivedMessage(data: string) {
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
        const batchData = await base64ToUInt8Array(parsed[2]);
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

async function base64ToUInt8Array(data: string): Promise<Uint8Array> {
  const dataUrl = "data:application/octet-binary;base64," + data;
  const response = await fetch(dataUrl);
  const arrayBuffer = await response.arrayBuffer();
  return new Uint8Array(arrayBuffer);
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
