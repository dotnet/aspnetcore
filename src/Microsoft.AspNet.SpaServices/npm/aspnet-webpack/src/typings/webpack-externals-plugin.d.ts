declare module 'webpack-externals-plugin' {
    import * as webpack from 'webpack';
    
    export interface ExternalsPluginOptions {
        type: string;
        include: webpack.LoaderCondition;
    }

    export default class ExternalsPlugin {
        constructor(options: ExternalsPluginOptions);
    }
}
