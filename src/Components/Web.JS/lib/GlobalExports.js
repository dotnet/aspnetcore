// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { navigateTo, internalFunctions as navigationManagerInternalFunctions } from './Services/NavigationManager';
import { domFunctions } from './DomWrapper';
import { Virtualize } from './Virtualize';
import { PageTitle } from './PageTitle';
import { registerCustomEventType } from './Rendering/Events/EventTypes';
import { InputFile } from './InputFile';
import { NavigationLock } from './NavigationLock';
import { getNextChunk, receiveDotNetDataStream } from './StreamingInterop';
import { RootComponentsFunctions } from './Rendering/JSRootComponents';
import { attachWebRendererInterop } from './Rendering/WebRendererInteropMethods';
export const Blazor = {
    navigateTo,
    registerCustomEventType,
    rootComponents: RootComponentsFunctions,
    _internal: {
        navigationManager: navigationManagerInternalFunctions,
        domWrapper: domFunctions,
        Virtualize,
        PageTitle,
        InputFile,
        NavigationLock,
        getJSDataStreamChunk: getNextChunk,
        receiveDotNetDataStream: receiveDotNetDataStream,
        attachWebRendererInterop,
    },
};
// Make the following APIs available in global scope for invocation from JS
window['Blazor'] = Blazor;
//# sourceMappingURL=GlobalExports.js.map