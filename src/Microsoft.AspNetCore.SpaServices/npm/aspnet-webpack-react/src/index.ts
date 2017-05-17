export { addReactHotModuleReplacementConfig } from './HotModuleReplacement';

// Temporarily alias addReactHotModuleReplacementConfig as addReactHotModuleReplacementBabelTransform for backward
// compatibility with aspnet-webpack 1.x. In aspnet-webpack 2.0, we can drop the old name (and also deprecate
// some other no-longer-supported functionality, such as LoadViaWebpack).
export { addReactHotModuleReplacementConfig as addReactHotModuleReplacementBabelTransform } from './HotModuleReplacement';
