import path from 'path';
import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';
import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import replace from '@rollup/plugin-replace';
import filesize from 'rollup-plugin-filesize';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

console.log(__dirname);

export default ({ environment }) => {

  var inputOutputMap = {
    'blazor.server': './src/Boot.Server.ts',
    'blazor.web': './src/Boot.Web.ts',
    'blazor.webassembly': './src/Boot.WebAssembly.ts',
    'blazor.webview': './src/Boot.WebView.ts',
  };

  /**
   * @type {import('rollup').RollupOptions}
   */
  const baseConfig = {
    output: {
      dir: path.join(__dirname, '/dist', environment === 'development' ? '/Debug' : '/Release'),
      format: 'cjs',
      sourcemap: true,
      entryFileNames: '[name].js',
    },
    plugins: [
      resolve(),
      commonjs(),
      typescript({
        tsconfig: path.join(__dirname, 'tsconfig.json')
      }),
      replace({
        'process.env.NODE_DEBUG': 'false',
        'Platform.isNode': 'false',
        preventAssignment: true
      }),
      terser({
        compress: {
          passes: 3
        },
        mangle: true,
        module: false,
        format: {
          ecma: 2020
        },
        keep_classnames: false,
        keep_fnames: false,
        toplevel: true
      })
      ,
      environment !== 'development' && filesize({ showMinifiedSize: true, showGzippedSize: true, showBrotliSize: true })
    ],
    treeshake: 'smallest',
    logLevel: 'silent'
  };

  return Object.entries(inputOutputMap).map(([output, input]) => {
    const config = {
      ...baseConfig,
      output: {
        ...baseConfig.output,
      },
      input: { [output]: input }
    };

    if (environment === 'development') {
      if (input.includes("WebView")) {
        config.output.sourcemap = 'inline';
      } else {
        config.output.sourcemap = true;
      }
    } else {
      config.output.sourcemap = false;
    }

    return config;
  });
};
