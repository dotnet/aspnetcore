import * as webpack from 'webpack';

const searchValue = /^\n*\/\/# sourceMappingURL=data:application\/json;charset=utf-8;base64,/;
const replaceValue = '\n//# sourceMappingURL=data:application/json;base64,';

/**
 * Wraps Webpack's built-in SourceMapDevToolPlugin with a post-processing step that makes inline source maps
 * work with Visual Studio's native debugger.
 *
 * The issue this fixes is that VS doesn't like to see 'charset=utf-8;' in the 'sourceMappingURL'. If that string
 * is present, VS will ignore the source map entirely. Until that VS bug is fixed, we can just strip out the charset
 * specifier from the URL. It's not needed because tools assume it's utf-8 anyway.
 */
export class SourceMapDevToolPlugin {
    /**
     * Constructs an instance of SourceMapDevToolPlugin.
     *
     * @param options Options that will be passed through to Webpack's native SourceMapDevToolPlugin.
     */
    constructor(private options?: any) {}

    protected apply(compiler: any) {
        // First, attach Webpack's native SourceMapDevToolPlugin, passing through the options
        const underlyingPlugin: any = new webpack.SourceMapDevToolPlugin(this.options);
        underlyingPlugin.apply(compiler);

        // Hook into the compilation right after the native SourceMapDevToolPlugin does
        compiler.plugin('compilation', compilation => {
            compilation.plugin('after-optimize-chunk-assets', chunks => {
                // Look for any compiled assets that might be an inline 'sourceMappingURL' source segment
                if (compilation.assets) {
                    Object.getOwnPropertyNames(compilation.assets).forEach(assetName => {
                        const asset = compilation.assets[assetName];
                        if (asset && asset.children instanceof Array) {
                            for (let index = 0; index < asset.children.length; index++) {
                                const assetChild = asset.children[index];
                                if (typeof assetChild === 'string') {
                                    // This asset is a source segment, so if it matches our regex, update it
                                    asset.children[index] = assetChild.replace(searchValue, replaceValue);
                                }
                            }
                        }
                    });
                }
            });
        });
    }
}
