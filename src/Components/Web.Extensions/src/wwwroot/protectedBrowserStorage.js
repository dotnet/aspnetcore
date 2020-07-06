(function () {
    window.protectedBrowserStorage = {
        get: (storeName, key) => window[storeName][key],
        set: (storeName, key, value) => { window[storeName][key] = value; },
        delete: (storeName, key) => { delete window[storeName][key]; }
    };
})();
