// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import typescript from '@rollup/plugin-typescript';
import { nodeResolve } from '@rollup/plugin-node-resolve';

// Builds the auto-pause JS initializer as an ES module shipped from the package wwwroot
// as a static web asset. Blazor auto-discovers `*.lib.module.js` from referenced RCLs.
export default {
  input: './src/js/autopause.lib.module.ts',
  output: {
    file: './wwwroot/autopause.lib.module.js',
    format: 'es',
    sourcemap: true,
  },
  plugins: [
    nodeResolve({ extensions: ['.ts', '.js'] }),
    typescript({
      tsconfig: false,
      compilerOptions: {
        target: 'es2020',
        module: 'esnext',
        moduleResolution: 'bundler',
        lib: ['dom', 'dom.iterable', 'es2020'],
        strict: true,
        types: [],
        sourceMap: true,
      },
      include: ['src/js/**/*.ts'],
    }),
  ],
};
