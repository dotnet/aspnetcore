declare const Blazor: any;

// This function is called by the framework because RegisterAsCustomElement sets it as the initializer function
(window as any).registerBlazorCustomElement = function defaultRegisterCustomElement(elementName: string, parameters: JSComponentParameter[]): void {
  customElements.define(elementName, class ConfiguredBlazorCustomElement extends BlazorCustomElement {
    static get observedAttributes() {
      return BlazorCustomElement.getObservedAttributes(parameters);
    }

    constructor() {
      super(parameters);
    }
  });
}

export class BlazorCustomElement extends HTMLElement {
  private _attributeMappings: { [attributeName: string]: JSComponentParameter };
  private _parameterValues: { [dotNetName: string]: any } = {};
  private _addRootComponentPromise: Promise<any>;
  private _hasPendingSetParameters = true; // The constructor will call setParameters, so it starts true
  private _isDisposed = false;
  private _disposalTimeoutHandle: any;

  public renderIntoElement = this;

  // Subclasses will need to call this if they want to retain the built-in behavior for knowing which
  // attribute names to observe, since they have to return it from a static function
  static getObservedAttributes(parameters: JSComponentParameter[]): string[] {
    return parameters.map(p => dasherize(p.name));
  }

  constructor(parameters: JSComponentParameter[]) {
    super();

    // Keep track of how we'll map the attributes to parameters
    this._attributeMappings = {};
    parameters.forEach(parameter => {
      const attributeName = dasherize(parameter.name);
      this._attributeMappings[attributeName] = parameter;
    });

    // Defer until end of execution cycle so that (1) we know the heap is unlocked, and (2) the initial parameter
    // values will be populated from the initial attributes before we send them to .NET
    this._addRootComponentPromise = Promise.resolve().then(() => {
      this._hasPendingSetParameters = false;
      return Blazor.rootComponents.add(this.renderIntoElement, this.localName, this._parameterValues);
    });

    // Also allow assignment of parameters via properties. This is the only way to set complex-typed values.
    for (const [attributeName, parameterInfo] of Object.entries(this._attributeMappings)) {
      const dotNetName = parameterInfo.name;
      Object.defineProperty(this, camelCase(dotNetName), {
        get: () => this._parameterValues[dotNetName],
        set: newValue => {
          if (this.hasAttribute(attributeName)) {
            // It's nice to keep the DOM in sync with the properties. This set a string representation
            // of the value, but this will get overwritten with the original typed value before we send it to .NET
            this.setAttribute(attributeName, newValue);
          }

          this._parameterValues[dotNetName] = newValue;
          this._supplyUpdatedParameters();
        }
      });
    }
  }

  connectedCallback() {
    if (this._isDisposed) {
      throw new Error(`Cannot connect component ${this.localName} to the document after it has been disposed.`);
    }

    clearTimeout(this._disposalTimeoutHandle);
  }

  disconnectedCallback() {
    this._disposalTimeoutHandle = setTimeout(async () => {
      this._isDisposed = true;
      const rootComponent = await this._addRootComponentPromise;
      rootComponent.dispose();
    }, 1000);
  }

  attributeChangedCallback(name: string, oldValue: string, newValue: string) {
    const parameterInfo = this._attributeMappings[name];
    if (parameterInfo) {
      this._parameterValues[parameterInfo.name] = BlazorCustomElement.parseAttributeValue(newValue, parameterInfo.type, parameterInfo.name);
      this._supplyUpdatedParameters();
    }
  }

  private async _supplyUpdatedParameters() {
    if (!this._hasPendingSetParameters) {
      this._hasPendingSetParameters = true;

      // Continuation from here will always be async, so at the earliest it will be at
      // the end of the current JS execution cycle
      const rootComponent = await this._addRootComponentPromise;
      if (!this._isDisposed) {
        const setParametersPromise = rootComponent.setParameters(this._parameterValues);
        this._hasPendingSetParameters = false; // We just snapshotted _parameterValues, so we need to start allowing new calls in case it changes further
        await setParametersPromise;
      }
    }
  }

  static parseAttributeValue(attributeValue: string, type: JSComponentParameterType, parameterName: string): any {
    switch (type) {
      case 'string':
        return attributeValue;
      case 'boolean':
        switch (attributeValue) {
          case 'true':
          case 'True':
            return true;
          case 'false':
          case 'False':
            return false;
          default:
            throw new Error(`Invalid boolean value '${attributeValue}' for parameter '${parameterName}'`);
        }
      case 'number':
        const number = Number(attributeValue);
        if (Number.isNaN(number)) {
          throw new Error(`Invalid number value '${attributeValue}' for parameter '${parameterName}'`);
        } else {
          return number;
        }
      case 'boolean?':
        return attributeValue ? BlazorCustomElement.parseAttributeValue(attributeValue, 'boolean', parameterName) : null;
      case 'number?':
        return attributeValue ? BlazorCustomElement.parseAttributeValue(attributeValue, 'number', parameterName) : null;
      case 'object':
        throw new Error(`The parameter '${parameterName}' accepts a complex-typed object so it cannot be set using an attribute. Try setting it as a element property instead.`);
      default:
        throw new Error(`Unknown type '${type}' for parameter '${parameterName}'`);
    }
  }
}

function dasherize(value: string): string {
  return camelCase(value).replace(/([A-Z])/g, "-$1").toLowerCase();
}

function camelCase(value: string): string {
  return value[0].toLowerCase() + value.substring(1);
}

interface JSComponentParameter {
  name: string;
  type: JSComponentParameterType;
}

// JSON-primitive types, plus for those whose .NET equivalent isn't nullable, a '?' to indicate nullability
// This allows custom element authors to coerce attribute strings into the appropriate type
type JSComponentParameterType = 'string' | 'boolean' | 'boolean?' | 'number' | 'number?' | 'object';
