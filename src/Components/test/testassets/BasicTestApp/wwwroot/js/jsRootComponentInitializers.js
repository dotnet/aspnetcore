(function () {
    const callLog = [];
    const state = window.__jsRootComponentInitializerState ??= {};

    window.myJsRootComponentInitializers = {
        captureRendererInterop(blazor) {
            const originalAttachWebRendererInterop = blazor._internal.attachWebRendererInterop;
            blazor._internal.attachWebRendererInterop = function (rendererId, interopMethods, ...configuration) {
                state.rendererInterop = interopMethods;
                return originalAttachWebRendererInterop.call(this, rendererId, interopMethods, ...configuration);
            };
        },

        testInitializer: function (name, parameters) {
            // Just keep track of the info we received so the E2E test can assert it was correct
            callLog.push({ name: name, parameters: parameters });
        },

        getCallLog() {
            return callLog;
        },

        async setRootComponentParameters(componentId, parameters) {
            try {
                await state.rendererInterop.invokeMethodAsync(
                    'SetRootComponentParameters',
                    componentId,
                    Object.keys(parameters).length,
                    parameters);
                return null;
            } catch (error) {
                return error.toString();
            }
        }
    };
})();
