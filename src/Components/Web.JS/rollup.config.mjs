import path from 'path';
import { fileURLToPath } from 'url';
import createBaseConfig from '../Shared.JS/rollup.config.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default createBaseConfig({
  inputOutputMap: {
    'blazor.server': './src/Boot.Server.ts',
    'blazor.web': './src/Boot.Web.ts',
    'blazor.webassembly': './src/Boot.WebAssembly.ts',
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

    if (input.includes("WebView")) {
      config.output.sourcemap = 'inline';
    } else if (environment === 'production' && (output === 'blazor.web' || output === 'blazor.webassembly')) {
      // Generate sourcemaps but don't emit sourcemap link comments for production bundles
      config.output.sourcemap = 'hidden';
    } else {
      config.output.sourcemap = true;
    }
  }
});
