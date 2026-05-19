// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { LogicalElement, toLogicalRootCommentElement } from '../Rendering/LogicalElements';
import { WebAssemblyComponentDescriptor } from '../Services/ComponentDescriptorDiscovery';
import { RootComponentManager } from '../Services/RootComponentManager';

export class WebAssemblyComponentAttacher {
  private componentManager: RootComponentManager<WebAssemblyComponentDescriptor>;

  public constructor(componentManager: RootComponentManager<WebAssemblyComponentDescriptor>) {
    this.componentManager = componentManager;
  }

  public resolveRegisteredElement(id: string): LogicalElement | undefined {
    const parsedId = Number.parseInt(id);
    if (!Number.isNaN(parsedId)) {
      const component = this.componentManager.resolveRootComponent(parsedId);
      return toLogicalRootCommentElement(component);
    } else {
      return undefined;
    }
  }

  public getParameterValues(id: number): string | undefined {
    return this.componentManager.initialComponents[id].parameterValues;
  }

  public getParameterDefinitions(id: number): string | undefined {
    return this.componentManager.initialComponents[id].parameterDefinitions;
  }

  public getTypeName(id: number): string {
    return this.componentManager.initialComponents[id].typeName;
  }

  public getAssembly(id: number): string {
    return this.componentManager.initialComponents[id].assembly;
  }

  public getCount(): number {
    return this.componentManager.initialComponents.length;
  }
}
