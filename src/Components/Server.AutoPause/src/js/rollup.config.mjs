import path from 'path';
import { fileURLToPath } from 'url';
import createBaseConfig from '../../../Shared.JS/rollup.config.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Builds the auto-pause JS initializer as an ES module. Blazor auto-discovers
// `*.lib.module.js` static web assets from referenced Razor class libraries.
export default createBaseConfig({
  inputOutputMap: {
    'Microsoft.AspNetCore.Components.Server.AutoPause.lib.module': './autopause.lib.module.ts',
  },
  dir: __dirname,
  updateConfig: (config, _environment, _output, _input) => {
    config.output.format = 'es';
  }
});
