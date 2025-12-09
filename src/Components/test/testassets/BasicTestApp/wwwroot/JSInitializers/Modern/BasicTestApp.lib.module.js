export function beforeWebStart() {
    // Ensure that initializers can be async by "yielding" execution
    return new Promise((resolve, reject) => {
        setTimeout(() => {
            appendElement('modern-before-web-start', 'Modern "beforeWebStart"');
            resolve();
        }, 0);
    });
}

export function afterWebStarted() {
    appendElement('modern-after-web-started', 'Modern "afterWebStarted"');
}

export function beforeWebAssemblyStart() {
    return new Promise((resolve, reject) => {
        setTimeout(() => {
            appendElement('modern-before-web-assembly-start', 'Modern "beforeWebAssemblyStart"');
            resolve();
        }, 0);
    });
}

export function afterWebAssemblyStarted() {
    appendElement('modern-after-web-assembly-started', 'Modern "afterWebAssemblyStarted"');
}

export function beforeServerStart(options) {
    // Ensure that initializers can be async by "yielding" execution
    return new Promise((resolve, reject) => {
        options.circuitHandlers.push({
            onCircuitOpened: () => {
                appendElement('modern-circuit-opened', 'Modern "circuitOpened"');
            },
            onCircuitClosed: () => appendElement('modern-circuit-closed', 'Modern "circuitClosed"')
        });
        appendElement('modern-before-server-start', 'Modern "beforeServerStart"');
        resolve();
    });
}

export function afterServerStarted() {
    appendElement('modern-after-server-started', 'Modern "afterServerStarted"');
}

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
