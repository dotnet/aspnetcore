export interface RenderToStringFunc {
    (callback: RenderToStringCallback, applicationBasePath: string, bootModule: BootModuleInfo, absoluteRequestUrl: string, requestPathAndQuery: string, customDataParameter: any, overrideTimeoutMilliseconds: number, requestPathBase: string): void;
}

export interface RenderToStringCallback {
    (error: any, result?: RenderResult): void;
}

export interface RenderToStringResult {
    html: string;
    statusCode?: number;
    globals?: { [key: string]: any };
}

export interface RedirectResult {
    redirectUrl: string;
}

export type RenderResult = RenderToStringResult | RedirectResult;

export interface BootFunc {
    (params: BootFuncParams): Promise<RenderResult>;
}

export interface BootFuncParams {
    location: any;              // e.g., Location object containing information '/some/path'
    origin: string;             // e.g., 'https://example.com:1234'
    url: string;                // e.g., '/some/path'
    baseUrl: string;            // e.g., '' or '/myVirtualDir'
    absoluteUrl: string;        // e.g., 'https://example.com:1234/some/path'
    domainTasks: Promise<any>;
    data: any;                  // any custom object passed through from .NET
}

export interface BootModuleInfo {
    moduleName: string;
    exportName?: string;
    webpackConfig?: string;
}
