// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { toLogicalRootCommentElement } from '../Rendering/LogicalElements';
export class WebAssemblyComponentAttacher {
    constructor(components) {
        this.preregisteredComponents = components;
        const componentsById = {};
        for (let index = 0; index < components.length; index++) {
            const component = components[index];
            componentsById[component.id] = component;
        }
        this.componentsById = componentsById;
    }
    resolveRegisteredElement(id) {
        const parsedId = Number.parseInt(id);
        if (!Number.isNaN(parsedId)) {
            return toLogicalRootCommentElement(this.componentsById[parsedId].start, this.componentsById[parsedId].end);
        }
        else {
            return undefined;
        }
    }
    getParameterValues(id) {
        return this.componentsById[id].parameterValues;
    }
    getParameterDefinitions(id) {
        return this.componentsById[id].parameterDefinitions;
    }
    getTypeName(id) {
        return this.componentsById[id].typeName;
    }
    getAssembly(id) {
        return this.componentsById[id].assembly;
    }
    getId(index) {
        return this.preregisteredComponents[index].id;
    }
    getCount() {
        return this.preregisteredComponents.length;
    }
}
//# sourceMappingURL=WebAssemblyComponentAttacher.js.map