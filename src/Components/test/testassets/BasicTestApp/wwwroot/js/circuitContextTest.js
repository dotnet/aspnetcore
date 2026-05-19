window.circuitContextTest = {
    invokeDotNetMethod: async (dotNetObject) => {
        await dotNetObject.invokeMethodAsync('InvokeDotNet');
    },
};
