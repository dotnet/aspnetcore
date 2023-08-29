(() => {
    const registeredCounters = [];

    window.enableCounterJsInterop = function (dotNetObject) {
        registeredCounters.push(dotNetObject);
    };

    window.incrementCounter = function (index) {
        const dotNetObject = registeredCounters[index];
        if (!dotNetObject) {
            console.log(`No counter with index ${index} exists.`);
        }
        dotNetObject.invokeMethodAsync('IncrementCount');
    }
})();
