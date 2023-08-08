// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { LogicalElement, toLogicalRootCommentElement } from '../Rendering/LogicalElements';
import { WebAssemblyComponentDescriptor } from '../Services/ComponentDescriptorDiscovery';
import { RootComponentManager } from '../Services/RootComponentManager';

export class WebAssemblyComponentAttacher {
  public preregisteredComponents: WebAssemblyComponentDescriptor[];

  private componentsById: { [index: number]: WebAssemblyComponentDescriptor };

  private componentManager: RootComponentManager<WebAssemblyComponentDescriptor>;

  public constructor(componentManager: RootComponentManager<WebAssemblyComponentDescriptor>) {
    this.componentManager = componentManager;
    this.componentsById = {};
    this.preregisteredComponents = componentManager.getFixedComponentArray();

    for (let index = 0; index < this.preregisteredComponents.length; index++) {
      const component = this.preregisteredComponents[index];
      this.componentsById[component.id] = component;
    }
  }

  public resolveRegisteredElement(id: string, componentId: number): LogicalElement | undefined {
    const parsedId = Number.parseInt(id);
    if (!Number.isNaN(parsedId)) {
      const component = this.componentManager.resolveRootComponent(parsedId, componentId);
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
