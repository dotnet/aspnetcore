// Currently this only deals with inserting streaming content into the DOM.
// Later this will be expanded to include:
//  - Progressive enhancement of navigation and form posting
//  - Preserving existing DOM elements in all the above
//  - The capabilities of Boot.Server.ts and Boot.WebAssembly.ts to handle insertion
//    of interactive components

import { shouldAutoStart } from './BootCommon';
import { attachStreamingRenderingListener } from './Rendering/StreamingRendering';

let started = false;

async function boot(): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  attachStreamingRenderingListener();
}

window['Blazor'] = { start: boot }; // Temporary API stub until we include interactive features

if (shouldAutoStart()) {
  boot();
}
