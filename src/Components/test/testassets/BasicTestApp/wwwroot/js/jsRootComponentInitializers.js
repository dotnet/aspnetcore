(function () {
    const callLog = [];

    window.myJsRootComponentInitializers = {
        testInitializer: function (name, parameters) {
            // Just keep track of the info we received so the E2E test can assert it was correct
            callLog.push({ name: name, parameters: parameters });
        },

        getCallLog() {
            return callLog;
        }
    };
})();
