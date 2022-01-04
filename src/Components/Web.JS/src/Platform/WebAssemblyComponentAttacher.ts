// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { LogicalElement, toLogicalRootCommentElement } from '../Rendering/LogicalElements';
import { WebAssemblyComponentDescriptor } from '../Services/ComponentDescriptorDiscovery';

export class WebAssemblyComponentAttacher {
  public preregisteredComponents: WebAssemblyComponentDescriptor[];

  private componentsById: { [index: number]: WebAssemblyComponentDescriptor };

  public constructor(components: WebAssemblyComponentDescriptor[]) {
    this.preregisteredComponents = components;
    const componentsById = {};
    for (let index = 0; index < components.length; index++) {
      const component = components[index];
      componentsById[component.id] = component;
    }
    this.componentsById = componentsById;
  }

  public resolveRegisteredElement(id: string): LogicalElement | undefined {
    const parsedId = Number.parseInt(id);
    if (!Number.isNaN(parsedId)) {
      return toLogicalRootCommentElement(this.componentsById[parsedId].start as Comment, this.componentsById[parsedId].end as Comment);
    } else {
      return undefined;
    }
  }

  public getParameterValues(id: number): string | undefined {
    return this.componentsById[id].parameterValues;
  }

  public getParameterDefinitions(id: number): string | undefined {
    return this.componentsById[id].parameterDefinitions;
  }

  public getTypeName(id: number): string {
    return this.componentsById[id].typeName;
  }

  public getAssembly(id: number): string {
    return this.componentsById[id].assembly;
  }

  public getId(index: number): number {
    return this.preregisteredComponents[index].id;
  }

  public getCount(): number {
    return this.preregisteredComponents.length;
  }
}
