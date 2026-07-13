import path from 'path';
import { fileURLToPath } from 'url';
import createBaseConfig from '../Shared.JS/rollup.config.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Injected before all module code in the Worker bundle so that
// @microsoft/dotnet-js-interop (which references `window` at module load time)
// can initialise inside a Web Worker where `window` is not defined.
const workerWindowPolyfill =
  'if(typeof window==="undefined"){Object.defineProperty(globalThis,"window",{value:globalThis,writable:true,configurable:true});}';

export default createBaseConfig({
  inputOutputMap: {
    '_framework/blazor.server': './src/Boot.Server.ts',
    '_framework/blazor.web': './src/Boot.Web.ts',
    '_framework/blazor.webassembly': './src/Boot.WebAssembly.ts',
    '_framework/blazor.webassembly.worker': './src/Boot.WebAssemblyWorker.ts',
    'blazor.webview': './src/Boot.WebView.ts',
  },
  dir: __dirname,
  updateConfig: (config, environment, output, input) => {
    config.plugins.push({
      name: 'Resolve dotnet.js dynamic import',
      resolveDynamicImport(source, importer) {
        if (source === './dotnet.js') {
          return { id: './dotnet.js', moduleSideEffects: false, external: 'relative' };
        }
        return null;
      }
    });

    if (output === '_framework/blazor.webassembly.worker') {
      // The Worker bundle needs a window polyfill injected before any module code.
      config.output.intro = workerWindowPolyfill;
      // Workers are loaded as ES modules when possible but must also work as
      // classic scripts (older browsers / some hosting environments).
      config.output.format = 'iife';
      if (environment === 'production') {
        config.output.sourcemap = 'hidden';
      } else {
        config.output.sourcemap = true;
      }
    } else if (input.includes("WebView")) {
      config.output.sourcemap = 'inline';
    } else if (environment === 'production' && (output === 'blazor.web' || output === 'blazor.webassembly')) {
      // Generate sourcemaps but don't emit sourcemap link comments for production bundles
      config.output.sourcemap = 'hidden';
    } else {
      config.output.sourcemap = true;
    }
  }
});
