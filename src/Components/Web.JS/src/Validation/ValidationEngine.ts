// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidatableElement, ValidatorRegistry } from './Validator';

export const validatableElementSelector = 'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]';

export class ValidationEngine {
  constructor(private validatorRegistry: ValidatorRegistry) {}

  validateField(_element: ValidatableElement): Promise<boolean> {
    return Promise.resolve(true);
  }

  validateForm(_form: HTMLFormElement): Promise<boolean> {
    return Promise.resolve(true);
  }
}
