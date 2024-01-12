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
  updateConfig: (config, environment, _, input) => {
    if (environment === 'development') {
      if (input.includes("WebView")) {
        config.output.sourcemap = 'inline';
      } else {
        config.output.sourcemap = true;
      }
    } else {
      config.output.sourcemap = false;
    }
  }
});
