export function beforeStart(options) {
    appendElement('classic-and-modern-before-start', 'Classic and modern "beforeStart"');
}

export function afterStarted() {
    appendElement('classic-and-modern-after-started', 'Classic and modern "afterStarted"');
}

export function beforeWebStart() {
    appendElement('classic-and-modern-before-web-start', 'Classic and modern "beforeWebStart"');
}

export function afterWebStarted() {
    appendElement('classic-and-modern-after-web-started', 'Classic and modern "afterWebStarted"');
}

export function beforeWebAssemblyStart() {
    appendElement('classic-and-modern-before-web-assembly-start', 'Classic and modern "beforeWebAssemblyStart"');
}

export function afterWebAssemblyStarted() {
    appendElement('classic-and-modern-after-web-assembly-started', 'Classic and modern "afterWebAssemblyStarted"');
}

export function beforeServerStart(options) {
    options.circuitHandlers.push({
        onCircuitOpened: () => {
            appendElement('classic-and-modern-circuit-opened', 'Classic and modern "circuitOpened"');
        },
        onCircuitClosed: () => appendElement('classic-and-modern-circuit-closed', 'Classic and modern "circuitClosed"')
    });
    appendElement('classic-and-modern-before-server-start', 'Classic and modern "beforeServerStart"');
}

export function afterServerStarted() {
    appendElement('classic-and-modern-after-server-started', 'Classic and modern "afterServerStarted"');
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
