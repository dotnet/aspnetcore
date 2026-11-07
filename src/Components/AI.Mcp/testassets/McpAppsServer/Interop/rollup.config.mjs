import { nodeResolve } from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import terser from '@rollup/plugin-terser';

const production = !!process.env.production;

export default {
  input: 'standard-app.js',
  output: {
    file: `dist/${production ? 'Release' : 'Debug'}/standard-app.js`,
    format: 'es',
    sourcemap: !production,
  },
  plugins: [
    nodeResolve({ browser: true, dedupe: ['zod'] }),
    commonjs(),
    ...(production ? [terser()] : []),
  ],
};
