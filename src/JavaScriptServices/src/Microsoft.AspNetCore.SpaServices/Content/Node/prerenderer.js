(function(e, a) { for(var i in a) e[i] = a[i]; }(exports, /******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, { enumerable: true, get: getter });
/******/ 		}
/******/ 	};
/******/
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = function(exports) {
/******/ 		if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 		}
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
/******/ 	};
/******/
/******/ 	// create a fake namespace object
/******/ 	// mode & 1: value is a module id, require it
/******/ 	// mode & 2: merge all properties of value into the ns
/******/ 	// mode & 4: return value when already ns object
/******/ 	// mode & 8|1: behave like require
/******/ 	__webpack_require__.t = function(value, mode) {
/******/ 		if(mode & 1) value = __webpack_require__(value);
/******/ 		if(mode & 8) return value;
/******/ 		if((mode & 4) && typeof value === 'object' && value && value.__esModule) return value;
/******/ 		var ns = Object.create(null);
/******/ 		__webpack_require__.r(ns);
/******/ 		Object.defineProperty(ns, 'default', { enumerable: true, value: value });
/******/ 		if(mode & 2 && typeof value != 'string') for(var key in value) __webpack_require__.d(ns, key, function(key) { return value[key]; }.bind(null, key));
/******/ 		return ns;
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = 0);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ (function(module, exports, __webpack_require__) {

module.exports = __webpack_require__(1);


/***/ }),
/* 1 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";

exports.__esModule = true;
var path = __webpack_require__(2);
// Separate declaration and export just to add type checking on function signature
exports.renderToString = renderToStringImpl;
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
function renderToStringImpl(callback, applicationBasePath, bootModule, absoluteRequestUrl, requestPathAndQuery, customDataParameter, overrideTimeoutMilliseconds) {
    try {
        var forceLegacy = isLegacyAspNetPrerendering();
        var renderToStringFunc = !forceLegacy && findRenderToStringFunc(applicationBasePath, bootModule);
        var isNotLegacyMode = renderToStringFunc && renderToStringFunc['isServerRenderer'];
        if (isNotLegacyMode) {
            // Current (non-legacy) mode - we invoke the exported function directly (instead of going through aspnet-prerendering)
            // It's type-safe to just apply the incoming args to this function, because we already type-checked that it's a RenderToStringFunc,
            // just like renderToStringImpl itself is.
            renderToStringFunc.apply(null, arguments);
        }
        else {
            // Legacy mode - just hand off execution to 'aspnet-prerendering' v1.x, which must exist in node_modules at runtime
            var aspNetPrerenderingV1RenderToString = __webpack_require__(3).renderToString;
            if (aspNetPrerenderingV1RenderToString) {
                aspNetPrerenderingV1RenderToString(callback, applicationBasePath, bootModule, absoluteRequestUrl, requestPathAndQuery, customDataParameter, overrideTimeoutMilliseconds);
            }
            else {
                callback('If you use aspnet-prerendering >= 2.0.0, you must update your server-side boot module to call createServerRenderer. '
                    + 'Either update your boot module code, or revert to aspnet-prerendering version 1.x');
            }
        }
    }
    catch (ex) {
        // Make sure loading errors are reported back to the .NET part of the app
        callback('Prerendering failed because of error: '
            + ex.stack
            + '\nCurrent directory is: '
            + process.cwd());
    }
}
;
function findBootModule(applicationBasePath, bootModule) {
    var bootModuleNameFullPath = path.resolve(applicationBasePath, bootModule.moduleName);
    if (bootModule.webpackConfig) {
        // If you're using asp-prerender-webpack-config, you're definitely in legacy mode
        return null;
    }
    else {
        return require(bootModuleNameFullPath);
    }
}
function findRenderToStringFunc(applicationBasePath, bootModule) {
    // First try to load the module
    var foundBootModule = findBootModule(applicationBasePath, bootModule);
    if (foundBootModule === null) {
        return null; // Must be legacy mode
    }
    // Now try to pick out the function they want us to invoke
    var renderToStringFunc;
    if (bootModule.exportName) {
        // Explicitly-named export
        renderToStringFunc = foundBootModule[bootModule.exportName];
    }
    else if (typeof foundBootModule !== 'function') {
        // TypeScript-style default export
        renderToStringFunc = foundBootModule["default"];
    }
    else {
        // Native default export
        renderToStringFunc = foundBootModule;
    }
    // Validate the result
    if (typeof renderToStringFunc !== 'function') {
        if (bootModule.exportName) {
            throw new Error("The module at " + bootModule.moduleName + " has no function export named " + bootModule.exportName + ".");
        }
        else {
            throw new Error("The module at " + bootModule.moduleName + " does not export a default function, and you have not specified which export to invoke.");
        }
    }
    return renderToStringFunc;
}
function isLegacyAspNetPrerendering() {
    var version = getAspNetPrerenderingPackageVersion();
    return version && /^1\./.test(version);
}
function getAspNetPrerenderingPackageVersion() {
    try {
        var packageEntryPoint = require.resolve('aspnet-prerendering');
        var packageDir = path.dirname(packageEntryPoint);
        var packageJsonPath = path.join(packageDir, 'package.json');
        var packageJson = require(packageJsonPath);
        return packageJson.version.toString();
    }
    catch (ex) {
        // Implies aspnet-prerendering isn't in node_modules at all (or node_modules itself doesn't exist,
        // which will be the case in production based on latest templates).
        return null;
    }
}


/***/ }),
/* 2 */
/***/ (function(module, exports) {

module.exports = require("path");

/***/ }),
/* 3 */
/***/ (function(module, exports) {

module.exports = require("aspnet-prerendering");

/***/ })
/******/ ])));