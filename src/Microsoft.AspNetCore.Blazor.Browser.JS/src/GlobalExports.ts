import { platform } from './Environment';
import { navigateTo, internalFunctions as uriHelperInternalFunctions } from './Services/UriHelper';
import { internalFunctions as httpInternalFunctions } from './Services/Http';
import { attachRootComponentToElement, renderBatch } from './Rendering/Renderer';
import { Pointer } from './Platform/Platform';
import { SharedMemoryRenderBatch } from './Rendering/RenderBatch/SharedMemoryRenderBatch';

if (typeof window !== 'undefined') {
  // When the library is loaded in a browser via a <script> element, make the
  // following APIs available in global scope for invocation from JS
  window['Blazor'] = {
    platform,
    navigateTo,

    _internal: {
      attachRootComponentToElement,
      renderBatch: (browserRendererId: number, batchAddress: Pointer) => renderBatch(browserRendererId, new SharedMemoryRenderBatch(batchAddress)),
      http: httpInternalFunctions,
      uriHelper: uriHelperInternalFunctions
    }
  };
}
