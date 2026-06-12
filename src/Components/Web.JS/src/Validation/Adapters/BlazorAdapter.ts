// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatableElement } from '../ValidationTypes';

const elementTagName = 'blazor-client-validation-data';

export function defineBlazorClientValidationDataElement(): void {
  if (customElements.get(elementTagName)) {
    return;
  }

  class BlazorClientValidationDataElement extends HTMLElement {
    static formAssociated = true;

    private internals: ElementInternals;

    private registeredInputs: ValidatableElement[] = [];

    constructor() {
      super();
      this.internals = this.attachInternals();
    }

    connectedCallback(): void {
      const _form = this.internals.form;
      // TODO
    }

    disconnectedCallback(): void {
      // TODO
    }
  }

  customElements.define(elementTagName, BlazorClientValidationDataElement);
}
