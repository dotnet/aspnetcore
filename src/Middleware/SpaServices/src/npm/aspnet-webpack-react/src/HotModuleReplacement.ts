import * as webpack from 'webpack';

const supportedTypeScriptLoaders = ['ts-loader', 'awesome-typescript-loader'];

export function addReactHotModuleReplacementConfig(webpackConfig: webpack.Configuration) {
    const moduleConfig = webpackConfig.module as webpack.Module;
    const moduleRules = moduleConfig.rules;
    if (!moduleRules) {
        return; // Unknown rules list format. Might be Webpack 1.x, which is not supported.
    }

    // Find the rule that loads TypeScript files, and prepend 'react-hot-loader/webpack'
    // to its array of loaders
    for (let ruleIndex = 0; ruleIndex < moduleRules.length; ruleIndex++) {
        // We only support NewUseRule (i.e., { use: ... }) because OldUseRule doesn't accept array values
        const rule = moduleRules[ruleIndex] as webpack.RuleSetRule;
        if (!rule.use) {
            continue;
        }

        // We're looking for the first 'use' value that's a TypeScript loader
        const loadersArray: webpack.RuleSetUseItem[] = rule.use instanceof Array ? rule.use : [rule.use as webpack.RuleSetUseItem];
        const isTypescriptLoader = supportedTypeScriptLoaders.some(typeScriptLoaderName => containsLoader(loadersArray, typeScriptLoaderName));
        if (!isTypescriptLoader) {
            continue;
        }

        break;
    }

    // Ensure the entrypoint is prefixed with 'react-hot-loader/patch' (unless it's already in there).
    // We only support entrypoints of the form { name: value } (not just 'name' or ['name'])
    // because that gives us a place to prepend the new value
    if (!webpackConfig.entry || typeof webpackConfig.entry === 'string' || webpackConfig.entry instanceof Array) {
        throw new Error('Cannot enable React HMR because \'entry\' in Webpack config is not of the form { name: value }');
    }
    const entryConfig = webpackConfig.entry as webpack.Entry;
    Object.getOwnPropertyNames(entryConfig).forEach(entrypointName => {
        if (typeof(entryConfig[entrypointName]) === 'string') {
            // Normalise to array
            entryConfig[entrypointName] = [entryConfig[entrypointName] as string];
        }
    });
}

function containsLoader(loadersArray: webpack.RuleSetUseItem[], loaderName: string) {
    return loadersArray.some(loader => {
        // Allow 'use' values to be either { loader: 'name' } or 'name'
        // No need to support legacy webpack.OldLoader
        const actualLoaderName = (loader as webpack.RuleSetLoader).loader || (loader as string);
        return actualLoaderName && new RegExp(`\\b${ loaderName }\\b`).test(actualLoaderName);
    });
}
