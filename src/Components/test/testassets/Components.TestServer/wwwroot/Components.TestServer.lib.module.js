export function beforeWebStart(options) {
    options.webAssembly.loadBootResource ??= (type, name) => `WasmMinimal/_framework/${name}`;
    if (new URLSearchParams(location.search).has('initializer-circuit-log-trace')) {
        options.circuit.logLevel = 0;
        window.circuitLogInitializerRan = true;
    }
    if (new URLSearchParams(location.search).has('initializer-preserve-dom')) {
        options.ssr.disableDomPreservation = false;
        window.preserveDomInitializerRan = true;
    }
    if (new URLSearchParams(location.search).has('initializer-inactivity-timeout')) {
        options.ssr.circuitInactivityTimeoutMs = 500;
        window.inactivityTimeoutInitializerRan = true;
    }
    if (new URLSearchParams(location.search).has('initializer-retry-interval')) {
        options.circuit.reconnectionOptions.retryIntervalMilliseconds = () => {
            window.retryIntervalInitializerRan = true;
            return 200;
        };
    }
}

export function beforeWebAssemblyStart() {
    appendElement('server--classic-and-modern-before-web-assembly-start', 'Server project Classic and modern "beforeWebAssemblyStart"');
}

export function afterWebAssemblyStarted() {
    appendElement('server--classic-and-modern-after-web-assembly-started', 'Server project Classic and modern "afterWebAssemblyStarted"');
}

// Duplicated in BasicTestApp\wwwroot\JSInitializers\ClassicAndModern\BasicTestApp.lib.module.js
function appendElement(id, text) {
    var content = document.getElementById('initializers-content');
    if (!content) {
        return;
    }
    var element = document.createElement('p');
    element.id = id;
    element.innerText = text;
    content.appendChild(element);
}
