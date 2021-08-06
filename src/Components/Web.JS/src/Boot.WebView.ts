import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { internalFunctions as navigationManagerFunctions } from './Services/NavigationManager';
import { startIpcReceiver } from './Platform/WebView/WebViewIpcReceiver';
import { sendAttachPage, sendBeginInvokeDotNetFromJS, sendEndInvokeJSFromDotNet, sendByteArray, sendLocationChanged } from './Platform/WebView/WebViewIpcSender';
import { JSInitializer as JSInitializer } from './JSInitializers';

let started = false;

async function boot(): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  const jsInitializersResponse = await fetch('_framework/blazor.modules.json', {
    method: 'GET',
    credentials: 'include',
    cache: 'no-cache'
  });

  const initializers: string[] = await jsInitializersResponse.json();
  const jsInitializer = new JSInitializer();
  await jsInitializer.importInitializersAsync(initializers, []);

  startIpcReceiver();

  DotNet.attachDispatcher({
    beginInvokeDotNetFromJS: sendBeginInvokeDotNetFromJS,
    endInvokeJSFromDotNet: sendEndInvokeJSFromDotNet,
    sendByteArray: sendByteArray,
  });

  navigationManagerFunctions.enableNavigationInterception();
  navigationManagerFunctions.listenForNavigationEvents(sendLocationChanged);

  sendAttachPage(navigationManagerFunctions.getBaseURI(), navigationManagerFunctions.getLocationHref());
  await jsInitializer.invokeAfterStartedCallbacks(Blazor);
}

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
