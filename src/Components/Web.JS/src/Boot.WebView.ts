// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { internalFunctions as navigationManagerFunctions } from './Services/NavigationManager';
import { startIpcReceiver } from './Platform/WebView/WebViewIpcReceiver';
import { sendAttachPage, sendBeginInvokeDotNetFromJS, sendEndInvokeJSFromDotNet, sendByteArray, sendLocationChanged, sendLocationChanging } from './Platform/WebView/WebViewIpcSender';
import { fetchAndInvokeInitializers } from './JSInitializers/JSInitializers.WebView';
import { receiveDotNetDataStream } from './StreamingInterop';
import { WebRendererId } from './Rendering/WebRendererId';

let started = false;

export let dispatcher: DotNet.ICallDispatcher;

async function boot(): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  dispatcher = DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: sendBeginInvokeDotNetFromJS,
    endInvokeJSFromDotNet: sendEndInvokeJSFromDotNet,
    sendByteArray: sendByteArray,
  });

  const jsInitializer = await fetchAndInvokeInitializers();

  startIpcReceiver();

  Blazor._internal.receiveWebViewDotNetDataStream = receiveWebViewDotNetDataStream;

  navigationManagerFunctions.enableNavigationInterception(WebRendererId.WebView);
  navigationManagerFunctions.listenForNavigationEvents(WebRendererId.WebView, sendLocationChanged, sendLocationChanging);

  sendAttachPage(navigationManagerFunctions.getBaseURI(), navigationManagerFunctions.getLocationHref());
  await jsInitializer.invokeAfterStartedCallbacks(Blazor);
}

function receiveWebViewDotNetDataStream(streamId: number, data: any, bytesRead: number, errorMessage: string): void {
  receiveDotNetDataStream(dispatcher, streamId, data, bytesRead, errorMessage);
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
