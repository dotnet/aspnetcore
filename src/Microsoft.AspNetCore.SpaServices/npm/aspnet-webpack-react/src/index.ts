export { addReactHotModuleReplacementConfig } from './HotModuleReplacement';

// Temporarily alias addReactHotModuleReplacementConfig as addReactHotModuleReplacementBabelTransform for backward
// compatibility with aspnet-webpack 1.x. In aspnet-webpack 2.0, we can drop the old name (and also deprecate
// some other no-longer-supported functionality, such as LoadViaWebpack).
export { addReactHotModuleReplacementConfig as addReactHotModuleReplacementBabelTransform } from './HotModuleReplacement';

// Workaround for #1066
//
// The issue is that @types/react-router@4.0.12 is incompatible with @types/react@15.0.29
// This is a problem because the ReactReduxSpa template that ships in 2.0.0-preview2 is pinned
// to @types/react@15.0.29 but does *not* declare a direct dependency on @types/react-router,
// which means we end up grabbing the latest @types/react-router.
//
// The temporary solution is for aspnet-webpack-react to add the following extra type information
// that patches the compatibility issue. The longer-term solution will be for the templates to
// pin versions of *every* package in the transitive closure, not just their direct dependencies.
//
// Note that for this workaround to work, the developer also needs 'aspnet-webpack-react' to be
// present in the 'types' array in tsconfig.json. We automate the task of adding that in the
// scripts/postinstall.js file in this package. Later, that action can be removed.

import * as React from 'react';
declare module 'react' {
    interface Component<P, S={}> {}
}
