import path from 'path';
import { fileURLToPath } from 'url';
import createBaseConfig from '../../../Shared.JS/rollup.config.mjs';

const directory = path.dirname(fileURLToPath(import.meta.url));

export default createBaseConfig({
  inputOutputMap: {
    'blazor.mcp': './Boot.Mcp.ts',
  },
  dir: directory,
  updateConfig: (config) => {
    config.output.sourcemap = true;
  },
});
