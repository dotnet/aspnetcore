import { navigateTo, internalFunctions as navigationManagerInternalFunctions } from './Services/NavigationManager';
import { protectedBrowserStorage } from './Services/ProtectedBrowserStorage';
import { attachRootComponentToElement } from './Rendering/Renderer';
import { domFunctions } from './DomWrapper';
import { setProfilingEnabled } from './Platform/Profiling';

// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = {
  navigateTo,

  _internal: {
    attachRootComponentToElement,
    navigationManager: navigationManagerInternalFunctions,
    protectedBrowserStorage,
    domWrapper: domFunctions,
    setProfilingEnabled: setProfilingEnabled,
  },
};
