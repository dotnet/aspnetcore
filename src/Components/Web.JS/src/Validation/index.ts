// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ES module facade for the client-side validation library. Importing this module
// has no side effects - the consumer is responsible for calling
// `createBlazorValidation()` to instantiate the service using the Blazor rules adapter.

export type {
  ValidationService,
  ValidationContext,
  ValidationResult,
  Validator,
  ValidatableElement,
} from './ValidationTypes';

export { createBlazorValidation, ensureNovalidateOnForms } from './Adapters/BlazorAdapter';
