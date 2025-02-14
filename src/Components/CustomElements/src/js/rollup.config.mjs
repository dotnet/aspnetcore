import path from 'path';
import { fileURLToPath } from 'url';
import createBaseConfig from '../../../Shared.JS/rollup.config.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default createBaseConfig({
  inputOutputMap: {
    'Microsoft.AspNetCore.Components.CustomElements.lib.module': './BlazorCustomElements.ts',
  },
  dir: __dirname,
  updateConfig: (config, environment, _, input) => {
    config.output.format = 'es';
  }
});
