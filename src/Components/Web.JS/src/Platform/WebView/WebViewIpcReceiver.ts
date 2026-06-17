// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { showErrorNotification } from '../../BootErrors';
import { OutOfProcessRenderBatch } from '../../Rendering/RenderBatch/OutOfProcessRenderBatch';
import { attachRootComponentToElement, renderBatch } from '../../Rendering/Renderer';
import { setApplicationIsTerminated, tryDeserializeMessage } from './WebViewIpcCommon';
import { sendRenderCompleted } from './WebViewIpcSender';
import { internalFunctions as navigationManagerFunctions } from '../../Services/NavigationManager';
import { dispatcher } from '../../Boot.WebView';
import { WebRendererId } from '../../Rendering/WebRendererId';

export function startIpcReceiver(): void {
  const messageHandlers = {

    'AttachToDocument': (componentId: number, elementSelector: string) => {
      attachRootComponentToElement(elementSelector, componentId, WebRendererId.WebView);
    },

    'RenderBatch': (batchId: number, batchDataBase64: string) => {
      try {
        const batchData = base64ToArrayBuffer(batchDataBase64);
        renderBatch(WebRendererId.WebView, new OutOfProcessRenderBatch(batchData));
        sendRenderCompleted(batchId, null);
      } catch (ex) {
        sendRenderCompleted(batchId, (ex as Error).toString());
      }
    },

    'NotifyUnhandledException': (message: string, stackTrace: string) => {
      setApplicationIsTerminated();
      console.error(`${message}\n${stackTrace}`);
      showErrorNotification();
    },

    'BeginInvokeJS': dispatcher.beginInvokeJSFromDotNet.bind(dispatcher),

    'EndInvokeDotNet': dispatcher.endInvokeDotNetFromJS.bind(dispatcher),

    'SendByteArrayToJS': receiveBase64ByteArray,

    'Navigate': navigationManagerFunctions.navigateTo,

    'Refresh': navigationManagerFunctions.refresh,

    'SetHasLocationChangingListeners': (hasListeners: boolean) => {
      navigationManagerFunctions.setHasLocationChangingListeners(WebRendererId.WebView, hasListeners);
    },

    'EndLocationChanging': navigationManagerFunctions.endLocationChanging,
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window.external as any).receiveMessage((message: string) => {
    const parsedMessage = tryDeserializeMessage(message);
    if (parsedMessage) {
      if (Object.prototype.hasOwnProperty.call(messageHandlers, parsedMessage.messageType)) {
        messageHandlers[parsedMessage.messageType].apply(null, parsedMessage.args);
      } else {
        throw new Error(`Unsupported IPC message type '${parsedMessage.messageType}'`);
      }
    }
  });
}

function receiveBase64ByteArray(id: number, base64Data: string) {
  const data = base64ToArrayBuffer(base64Data);
  dispatcher.receiveByteArray(id, data);
}

// https://stackoverflow.com/a/21797381
// TODO: If the data is large, consider switching over to the native decoder as in https://stackoverflow.com/a/54123275
// But don't force it to be async all the time. Yielding execution leads to perceptible lag.
function base64ToArrayBuffer(base64: string): Uint8Array {
  const binaryString = atob(base64);
  const length = binaryString.length;
  const result = new Uint8Array(length);
  for (let i = 0; i < length; i++) {
    result[i] = binaryString.charCodeAt(i);
  }
  return result;
}
