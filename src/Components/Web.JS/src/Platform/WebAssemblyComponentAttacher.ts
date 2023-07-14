// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { LogicalElement, toLogicalRootCommentElement } from '../Rendering/LogicalElements';
import { WebAssemblyComponentDescriptor } from '../Services/ComponentDescriptorDiscovery';
import { RootComponentManager } from '../Services/RootComponentManager';

export class WebAssemblyComponentAttacher {
  public preregisteredComponents: WebAssemblyComponentDescriptor[];

  private componentsById: { [index: number]: WebAssemblyComponentDescriptor };

  private rootComponentManager?: RootComponentManager;

  public constructor(components: WebAssemblyComponentDescriptor[] | RootComponentManager) {
    this.componentsById = {};
    if (components instanceof RootComponentManager) {
      this.preregisteredComponents = [];
      this.rootComponentManager = components;
    } else {
      this.preregisteredComponents = components;
      for (let index = 0; index < components.length; index++) {
        const component = components[index];
        this.componentsById[component.id] = component;
      }
    }
  }

  public resolveRegisteredElement(id: string, componentId: number): LogicalElement | undefined {
    const parsedId = Number.parseInt(id);
    if (!Number.isNaN(parsedId)) {
      const component = this.rootComponentManager?.resolveRootComponent(parsedId, componentId) || this.componentsById[parsedId];
      return toLogicalRootCommentElement(component);
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
