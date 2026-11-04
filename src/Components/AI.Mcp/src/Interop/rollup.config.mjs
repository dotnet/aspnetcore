import { nodeResolve } from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import terser from '@rollup/plugin-terser';

const production = !!process.env.production;

export default {
  input: ['mcp-host.js', 'mcp-proxy.js'],
  output: {
    dir: `dist/${production ? 'Release' : 'Debug'}`,
    format: 'es',
    sourcemap: !production,
  },
  plugins: [
    nodeResolve({ browser: true, dedupe: ['zod'] }),
    commonjs(),
    ...(production ? [terser()] : []),
  ],
};
