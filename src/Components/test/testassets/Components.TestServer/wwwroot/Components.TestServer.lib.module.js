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
