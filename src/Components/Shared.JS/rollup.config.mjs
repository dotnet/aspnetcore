import path from 'path';
import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';
import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import replace from '@rollup/plugin-replace';
import filesize from 'rollup-plugin-filesize';
import { env } from 'process';

/**
 * @callback UpdateConfigFunction
 * @param {import('rollup').RollupOptions} config - The Rollup configuration to update.
 * @param {'development' | 'production' } environment - The environment we are compiling for ().
 * @param {string} output - The bundle we are generating.
 * @param {string} input - The entry point for the bundle.
 */

/**
 * @typedef {Object} BaseOptions
 * @property {Object.<string, string>} inputOutputMap - An object that maps a string key to a string.
 * @property {string} dir - The directory for the config that we are creating.
 * @property {UpdateConfigFunction} updateConfig - A function that updates the configuration.
 */

/**
 *
 * @param {BaseOptions} options
 * @returns
 */
export default function createBaseConfig({ inputOutputMap, dir, updateConfig }) {

  return ({ environment }) => {

    /**
     * @type {import('rollup').RollupOptions}
     */
    const baseConfig = {
      output: {
        dir: path.join(dir, '/dist', environment === 'development' ? '/Debug' : '/Release'),
        format: 'iife',
        sourcemap: environment === 'development' ? true : false,
        entryFileNames: '[name].js',
      },
      plugins: [
        resolve(),
        commonjs(),
        typescript({
          tsconfig: path.join(dir, 'tsconfig.json')
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
        }),
        // Check the ContinuousIntegrationBuild environment variable to determine if we should show the file size.
        env.ContinuousIntegrationBuild !== 'true' && environment !== 'development' && filesize({ showMinifiedSize: true, showGzippedSize: true, showBrotliSize: true })
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
        plugins: [
          ...baseConfig.plugins
        ],
        input: { [output]: input }
      };

      updateConfig(config, environment, output, input);

      return config;
    });
  };
};
