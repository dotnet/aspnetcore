// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ValidationProvider } from './Types';

export class ValidationEngine {
  private providers: Map<string, ValidationProvider> = new Map();

  addProvider(name: string, provider: ValidationProvider): void {
    this.providers.set(name, provider);
  }

  getProvider(name: string): ValidationProvider | undefined {
    return this.providers.get(name);
  }

  hasProvider(name: string): boolean {
    return this.providers.has(name);
  }
}
