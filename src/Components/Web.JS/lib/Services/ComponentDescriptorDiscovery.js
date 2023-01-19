// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
export function discoverComponents(document, type) {
    switch (type) {
        case 'webassembly':
            return discoverWebAssemblyComponents(document);
        case 'server':
            return discoverServerComponents(document);
    }
}
function discoverServerComponents(document) {
    const componentComments = resolveComponentComments(document, 'server');
    const discoveredComponents = [];
    for (let i = 0; i < componentComments.length; i++) {
        const componentComment = componentComments[i];
        const entry = new ServerComponentDescriptor(componentComment.type, componentComment.start, componentComment.end, componentComment.sequence, componentComment.descriptor);
        discoveredComponents.push(entry);
    }
    return discoveredComponents.sort((a, b) => a.sequence - b.sequence);
}
const blazorStateCommentRegularExpression = /^\s*Blazor-Component-State:(?<state>[a-zA-Z0-9+/=]+)$/;
export function discoverPersistedState(node) {
    var _a;
    if (node.nodeType === Node.COMMENT_NODE) {
        const content = node.textContent || '';
        const parsedState = blazorStateCommentRegularExpression.exec(content);
        const value = parsedState && parsedState.groups && parsedState.groups['state'];
        if (value) {
            (_a = node.parentNode) === null || _a === void 0 ? void 0 : _a.removeChild(node);
        }
        return value;
    }
    if (!node.hasChildNodes()) {
        return;
    }
    const nodes = node.childNodes;
    for (let index = 0; index < nodes.length; index++) {
        const candidate = nodes[index];
        const result = discoverPersistedState(candidate);
        if (result) {
            return result;
        }
    }
    return;
}
function discoverWebAssemblyComponents(document) {
    const componentComments = resolveComponentComments(document, 'webassembly');
    const discoveredComponents = [];
    for (let i = 0; i < componentComments.length; i++) {
        const componentComment = componentComments[i];
        const entry = new WebAssemblyComponentDescriptor(componentComment.type, componentComment.start, componentComment.end, componentComment.assembly, componentComment.typeName, componentComment.parameterDefinitions, componentComment.parameterValues);
        discoveredComponents.push(entry);
    }
    return discoveredComponents.sort((a, b) => a.id - b.id);
}
function resolveComponentComments(node, type) {
    if (!node.hasChildNodes()) {
        return [];
    }
    const result = [];
    const childNodeIterator = new ComponentCommentIterator(node.childNodes);
    while (childNodeIterator.next() && childNodeIterator.currentElement) {
        const componentComment = getComponentComment(childNodeIterator, type);
        if (componentComment) {
            result.push(componentComment);
        }
        else {
            const childResults = resolveComponentComments(childNodeIterator.currentElement, type);
            for (let j = 0; j < childResults.length; j++) {
                const childResult = childResults[j];
                result.push(childResult);
            }
        }
    }
    return result;
}
const blazorCommentRegularExpression = new RegExp(/^\s*Blazor:[^{]*(?<descriptor>.*)$/);
function getComponentComment(commentNodeIterator, type) {
    const candidateStart = commentNodeIterator.currentElement;
    if (!candidateStart || candidateStart.nodeType !== Node.COMMENT_NODE) {
        return;
    }
    if (candidateStart.textContent) {
        const definition = blazorCommentRegularExpression.exec(candidateStart.textContent);
        const json = definition && definition.groups && definition.groups['descriptor'];
        if (json) {
            try {
                const componentComment = parseCommentPayload(json);
                switch (type) {
                    case 'webassembly':
                        return createWebAssemblyComponentComment(componentComment, candidateStart, commentNodeIterator);
                    case 'server':
                        return createServerComponentComment(componentComment, candidateStart, commentNodeIterator);
                }
            }
            catch (error) {
                throw new Error(`Found malformed component comment at ${candidateStart.textContent}`);
            }
        }
        else {
            return;
        }
    }
}
function parseCommentPayload(json) {
    const payload = JSON.parse(json);
    const { type } = payload;
    if (type !== 'server' && type !== 'webassembly') {
        throw new Error(`Invalid component type '${type}'.`);
    }
    return payload;
}
function createServerComponentComment(payload, start, iterator) {
    const { type, descriptor, sequence, prerenderId } = payload;
    if (type !== 'server') {
        return undefined;
    }
    if (!descriptor) {
        throw new Error('descriptor must be defined when using a descriptor.');
    }
    if (sequence === undefined) {
        throw new Error('sequence must be defined when using a descriptor.');
    }
    if (!Number.isInteger(sequence)) {
        throw new Error(`Error parsing the sequence '${sequence}' for component '${JSON.stringify(payload)}'`);
    }
    if (!prerenderId) {
        return {
            type,
            sequence: sequence,
            descriptor,
            start,
        };
    }
    else {
        const end = getComponentEndComment(prerenderId, iterator);
        if (!end) {
            throw new Error(`Could not find an end component comment for '${start}'`);
        }
        return {
            type,
            sequence,
            descriptor,
            start,
            prerenderId,
            end,
        };
    }
}
function createWebAssemblyComponentComment(payload, start, iterator) {
    const { type, assembly, typeName, parameterDefinitions, parameterValues, prerenderId } = payload;
    if (type !== 'webassembly') {
        return undefined;
    }
    if (!assembly) {
        throw new Error('assembly must be defined when using a descriptor.');
    }
    if (!typeName) {
        throw new Error('typeName must be defined when using a descriptor.');
    }
    if (!prerenderId) {
        return {
            type,
            assembly,
            typeName,
            // Parameter definitions and values come Base64 encoded from the server, since they contain random data and can make the
            // comment invalid. We could unencode them in .NET Code, but that would be slower to do and we can leverage the fact that
            // JS provides a native function that will be much faster and that we are doing this work while we are fetching
            // blazor.boot.json
            parameterDefinitions: parameterDefinitions && atob(parameterDefinitions),
            parameterValues: parameterValues && atob(parameterValues),
            start,
        };
    }
    else {
        const end = getComponentEndComment(prerenderId, iterator);
        if (!end) {
            throw new Error(`Could not find an end component comment for '${start}'`);
        }
        return {
            type,
            assembly,
            typeName,
            // Same comment as above.
            parameterDefinitions: parameterDefinitions && atob(parameterDefinitions),
            parameterValues: parameterValues && atob(parameterValues),
            start,
            prerenderId,
            end,
        };
    }
}
function getComponentEndComment(prerenderedId, iterator) {
    while (iterator.next() && iterator.currentElement) {
        const node = iterator.currentElement;
        if (node.nodeType !== Node.COMMENT_NODE) {
            continue;
        }
        if (!node.textContent) {
            continue;
        }
        const definition = blazorCommentRegularExpression.exec(node.textContent);
        const json = definition && definition[1];
        if (!json) {
            continue;
        }
        validateEndComponentPayload(json, prerenderedId);
        return node;
    }
    return undefined;
}
function validateEndComponentPayload(json, prerenderedId) {
    const payload = JSON.parse(json);
    if (Object.keys(payload).length !== 1) {
        throw new Error(`Invalid end of component comment: '${json}'`);
    }
    const prerenderedEndId = payload.prerenderId;
    if (!prerenderedEndId) {
        throw new Error(`End of component comment must have a value for the prerendered property: '${json}'`);
    }
    if (prerenderedEndId !== prerenderedId) {
        throw new Error(`End of component comment prerendered property must match the start comment prerender id: '${prerenderedId}', '${prerenderedEndId}'`);
    }
}
class ComponentCommentIterator {
    constructor(childNodes) {
        this.childNodes = childNodes;
        this.currentIndex = -1;
        this.length = childNodes.length;
    }
    next() {
        this.currentIndex++;
        if (this.currentIndex < this.length) {
            this.currentElement = this.childNodes[this.currentIndex];
            return true;
        }
        else {
            this.currentElement = undefined;
            return false;
        }
    }
}
export class ServerComponentDescriptor {
    constructor(type, start, end, sequence, descriptor) {
        this.type = type;
        this.start = start;
        this.end = end;
        this.sequence = sequence;
        this.descriptor = descriptor;
    }
    toRecord() {
        const result = { type: this.type, sequence: this.sequence, descriptor: this.descriptor };
        return result;
    }
}
export class WebAssemblyComponentDescriptor {
    constructor(type, start, end, assembly, typeName, parameterDefinitions, parameterValues) {
        this.id = WebAssemblyComponentDescriptor.globalId++;
        this.type = type;
        this.assembly = assembly;
        this.typeName = typeName;
        this.parameterDefinitions = parameterDefinitions;
        this.parameterValues = parameterValues;
        this.start = start;
        this.end = end;
    }
}
WebAssemblyComponentDescriptor.globalId = 1;
//# sourceMappingURL=ComponentDescriptorDiscovery.js.map