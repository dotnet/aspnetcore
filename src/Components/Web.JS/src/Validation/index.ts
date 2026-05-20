// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ES module facade for the client-side validation library. Importing this module
// has no side effects - the consumer is responsible for calling
// `createValidationService()` to instantiate the service.

export { createValidationService } from './ValidationService';
export type {
  ValidationService,
  ValidationOptions,
  ValidationContext,
  ValidationResult,
  Validator,
  ValidatableElement,
} from './ValidationTypes';
