import { BootModuleInfo, RenderToStringFunc, RenderToStringCallback } from '../npm/aspnet-prerendering/src/PrerenderingInterfaces';
import * as path from 'path';
declare var __non_webpack_require__;

// Separate declaration and export just to add type checking on function signature
export const renderToString: RenderToStringFunc = renderToStringImpl;

// This function is invoked by .NET code (via NodeServices). Its job is to hand off execution to the application's
// prerendering boot function. It can operate in two modes:
// [1] Legacy mode
//     This is for backward compatibility with projects created with templates older than the generator version 0.6.0.
//     In this mode, we don't really do anything here - we just load the 'aspnet-prerendering' NPM module (which must
//     exist in node_modules, and must be v1.x (not v2+)), and pass through all the parameters to it. Code in
//     'aspnet-prerendering' v1.x will locate the boot function and invoke it.
//     The drawback to this mode is that, for it to work, you have to deploy node_modules to production.
// [2] Current mode
//     This is for projects created with the Yeoman generator 0.6.0+ (or projects manually updated). In this mode,
//     we don't invoke 'require' at runtime at all. All our dependencies are bundled into the NuGet package, so you
//     don't have to deploy node_modules to production.
// To determine whether we're in mode [1] or [2], the code locates your prerendering boot function, and checks whether
// a certain flag is attached to the function instance.
function renderToStringImpl(callback: RenderToStringCallback, applicationBasePath: string, bootModule: BootModuleInfo, absoluteRequestUrl: string, requestPathAndQuery: string, customDataParameter: any, overrideTimeoutMilliseconds: number) {
    try {
        const forceLegacy = isLegacyAspNetPrerendering();
        const renderToStringFunc = !forceLegacy && findRenderToStringFunc(applicationBasePath, bootModule);
        const isNotLegacyMode = renderToStringFunc && renderToStringFunc['isServerRenderer'];

        if (isNotLegacyMode) {
            // Current (non-legacy) mode - we invoke the exported function directly (instead of going through aspnet-prerendering)
            // It's type-safe to just apply the incoming args to this function, because we already type-checked that it's a RenderToStringFunc,
            // just like renderToStringImpl itself is.
            renderToStringFunc.apply(null, arguments);
        } else {
            // Legacy mode - just hand off execution to 'aspnet-prerendering' v1.x, which must exist in node_modules at runtime
            const aspNetPrerenderingV1RenderToString = require('aspnet-prerendering').renderToString;
            if (aspNetPrerenderingV1RenderToString) {
                aspNetPrerenderingV1RenderToString(callback, applicationBasePath, bootModule, absoluteRequestUrl, requestPathAndQuery, customDataParameter, overrideTimeoutMilliseconds);
            } else {
                callback('If you use aspnet-prerendering >= 2.0.0, you must update your server-side boot module to call createServerRenderer. '
                    + 'Either update your boot module code, or revert to aspnet-prerendering version 1.x');
            }
        }
    } catch (ex) {
        // Make sure loading errors are reported back to the .NET part of the app
        callback(
            'Prerendering failed because of error: '
            + ex.stack
            + '\nCurrent directory is: '
            + process.cwd()
        );
    }
};

function findBootModule(applicationBasePath: string, bootModule: BootModuleInfo): any {
    const bootModuleNameFullPath = path.resolve(applicationBasePath, bootModule.moduleName);
    if (bootModule.webpackConfig) {
        // If you're using asp-prerender-webpack-config, you're definitely in legacy mode
        return null;
    } else {
        return __non_webpack_require__(bootModuleNameFullPath);
    }
}

function findRenderToStringFunc(applicationBasePath: string, bootModule: BootModuleInfo): RenderToStringFunc {
    // First try to load the module
    const foundBootModule = findBootModule(applicationBasePath, bootModule);
    if (foundBootModule === null) {
        return null; // Must be legacy mode
    }

    // Now try to pick out the function they want us to invoke
    let renderToStringFunc: RenderToStringFunc;
    if (bootModule.exportName) {
        // Explicitly-named export
        renderToStringFunc = foundBootModule[bootModule.exportName];
    } else if (typeof foundBootModule !== 'function') {
        // TypeScript-style default export
        renderToStringFunc = foundBootModule.default;
    } else {
        // Native default export
        renderToStringFunc = foundBootModule;
    }

    // Validate the result
    if (typeof renderToStringFunc !== 'function') {
        if (bootModule.exportName) {
            throw new Error(`The module at ${ bootModule.moduleName } has no function export named ${ bootModule.exportName }.`);
        } else {
            throw new Error(`The module at ${ bootModule.moduleName } does not export a default function, and you have not specified which export to invoke.`);
        }
    }

    return renderToStringFunc;
}

function isLegacyAspNetPrerendering() {
    const version = getAspNetPrerenderingPackageVersion();
    return version && /^1\./.test(version);
}

function getAspNetPrerenderingPackageVersion() {
    try {
        const packageEntryPoint = __non_webpack_require__.resolve('aspnet-prerendering');
        const packageDir = path.dirname(packageEntryPoint);
        const packageJsonPath = path.join(packageDir, 'package.json');
        const packageJson = __non_webpack_require__(packageJsonPath);
        return packageJson.version.toString();
    } catch(ex) {
        // Implies aspnet-prerendering isn't in node_modules at all (or node_modules itself doesn't exist,
        // which will be the case in production based on latest templates).
        return null;
    }
}
