import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { internalFunctions as navigationManagerFunctions } from './Services/NavigationManager';
import { setEventDispatcher } from './Rendering/Events/EventDispatcher';
import { startIpcReceiver } from './Platform/WebView/WebViewIpcReceiver';
import { dispatchBrowserEvent, sendAttachPage } from './Platform/WebView/WebViewIpcSender';

let started = false;

async function boot(): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  startIpcReceiver();
  sendAttachPage(navigationManagerFunctions.getBaseURI(), navigationManagerFunctions.getLocationHref());
}

setEventDispatcher((descriptor, args) => {
  dispatchBrowserEvent(JSON.stringify(descriptor), JSON.stringify(args));
});

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
