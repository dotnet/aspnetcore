import { DotNet } from '@microsoft/dotnet-js-interop';
import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import * as Ipc from './Platform/WebView/WebViewIpc';
import { internalFunctions as navigationManagerFunctions } from './Services/NavigationManager';
import { setEventDispatcher } from './Rendering/Events/EventDispatcher';

let started = false;

async function boot(): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  Ipc.startListener();
  Ipc.sendAttachPage(navigationManagerFunctions.getBaseURI(), navigationManagerFunctions.getLocationHref());
}

setEventDispatcher((descriptor, args) => {
  Ipc.dispatchBrowserEvent(JSON.stringify(descriptor), JSON.stringify(args));
});

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
