import { ComponentProxy } from '../Rendering/DynamicComponents';
import { LogicalElement, toLogicalElement, toLogicalRootCommentElement } from '../Rendering/LogicalElements';
import { WebAssemblyComponentDescriptor } from '../Services/ComponentDescriptorDiscovery';

export class WebAssemblyComponentAttacher {
  public preregisteredComponents: WebAssemblyComponentDescriptor[];

  private preregisteredComponentsById: { [index: number]: WebAssemblyComponentDescriptor };
  private dynamicComponentsById: { [index: number]: ComponentProxy };
  private dynamicComponentId: number;

  public constructor(components: WebAssemblyComponentDescriptor[]) {
    this.preregisteredComponents = components;
    this.dynamicComponentsById = [];

    const componentsById = {};
    for (let index = 0; index < components.length; index++) {
      const component = components[index];
      componentsById[component.id] = component;
    }
    this.preregisteredComponentsById = componentsById;
    this.dynamicComponentsById = {};
    this.dynamicComponentId = 1;
  }

  public resolveRegisteredElement(id: string): LogicalElement | undefined {
    const parsedId = Number.parseInt(id);
    if (!Number.isNaN(parsedId)) {
      const preregistered = this.preregisteredComponentsById[parsedId];
      if (preregistered) {
        return toLogicalRootCommentElement(preregistered.start as Comment, preregistered.end as Comment);
      } else {
        const dynamicComponent = this.dynamicComponentsById[parsedId];
        if (dynamicComponent) {
          return toLogicalElement(dynamicComponent.element, /* allowExistingContents */ true);
        } else {
          return undefined;
        }
      }
    } else {
      return undefined;
    }
  }

  registerDynamicComponent(rootElement: HTMLElement) : ComponentProxy {
    const nextId = this.dynamicComponentId++;
    const proxy = new ComponentProxy(nextId, rootElement);
    this.dynamicComponentsById[nextId] = proxy;

    return proxy;
  }

  public isPreregisteredComponent(element: WebAssemblyComponentDescriptor | ComponentProxy): element is WebAssemblyComponentDescriptor {
    return element?.['start'];
  }

  public getParameterValues(id: number): string | undefined {
    const component = this.preregisteredComponentsById[id];
    if (this.isPreregisteredComponent(component)) {
      return component.parameterValues;
    } else {
      throw new Error('The component is not registered.');
    }
  }

  public getParameterDefinitions(id: number): string | undefined {
    const component = this.preregisteredComponentsById[id];
    if (this.isPreregisteredComponent(component)) {
      return component.parameterDefinitions;
    } else {
      throw new Error('The component is not registered.');
    }
  }

  public getTypeName(id: number): string {
    const component = this.preregisteredComponentsById[id];
    if (this.isPreregisteredComponent(component)) {
      return component.typeName;
    } else {
      throw new Error('The component is not registered.');
    }
  }

  public getAssembly(id: number): string {
    const component = this.preregisteredComponentsById[id];
    if (this.isPreregisteredComponent(component)) {
      return component.assembly;
    } else {
      throw new Error('The component is not registered.');
    }
  }

  public getId(index: number): number {
    return this.preregisteredComponents[index].id;
  }

  public getCount(): number {
    return this.preregisteredComponents.length;
  }
}
