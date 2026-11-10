import path from 'path';
import { fileURLToPath } from 'url';
import createBaseConfig from '../../../Shared.JS/rollup.config.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// The .NET side imports the bundle as an ES module, so it must not end in .lib.module.js,
// which Blazor would load as a JS initializer.
export default createBaseConfig({
  inputOutputMap: {
    'Microsoft.AspNetCore.Components.Platform': './Platform.ts',
  },
  dir: __dirname,
  updateConfig: (config, _environment, _output, _input) => {
    config.output.format = 'es';
  }
});
