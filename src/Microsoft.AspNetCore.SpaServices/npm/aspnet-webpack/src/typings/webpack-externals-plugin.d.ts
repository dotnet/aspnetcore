import * as webpack from 'webpack';

export namespace webpackexternals {
    export interface ExternalsPluginOptions {
        type: string;
        include: webpack.LoaderCondition;
    }

    export class ExternalsPlugin {
        constructor(options: ExternalsPluginOptions);
    }
}
