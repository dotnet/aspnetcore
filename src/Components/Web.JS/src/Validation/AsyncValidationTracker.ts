// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AsyncValidationTracker, ValidatableElement, ValidationResult } from './ValidationTypes';

const PENDING_CSS = 'validation-pending';

export class DefaultAsyncValidationTracker implements AsyncValidationTracker {
  // Per-element pending validators: element → Set<validatorName>
  private pending = new Map<ValidatableElement, Set<string>>();

  // Version counter for staleness detection: "elementName:validatorName" → version
  private versions = new Map<string, number>();

  // AbortControllers for in-flight async validators: key → AbortController
  private controllers = new Map<string, AbortController>();

  createSignal(element: ValidatableElement, validatorName: string): AbortSignal {
    const key = this.makeKey(element, validatorName);

    // Abort previous signal for the same element+validator
    this.controllers.get(key)?.abort();

    const controller = new AbortController();
    this.controllers.set(key, controller);

    return controller.signal;
  }

  track(
    element: ValidatableElement,
    validatorName: string,
    promise: Promise<ValidationResult>,
    onResolved: () => void,
  ): void {
    const key = this.makeKey(element, validatorName);

    // Increment version - any in-flight callback for a previous version is stale
    const version = (this.versions.get(key) ?? 0) + 1;
    this.versions.set(key, version);

    // Add to pending set (idempotent - no CSS flicker when replacing a promise)
    this.addPending(element, validatorName);

    const handleSettled = () => {
      if (this.versions.get(key) !== version) {
        return; // superseded - ignore
      }

      this.removePending(element, validatorName);
      onResolved();
    };

    promise.then(handleSettled, handleSettled);
  }

  hasPending(): boolean {
    return this.pending.size > 0;
  }

  clear(element?: ValidatableElement): void {
    if (element) {
      // Abort and remove controllers for this element
      for (const [key, controller] of this.controllers) {
        if (key.startsWith(element.name + ':')) {
          controller.abort();
          this.controllers.delete(key);
        }
      }
      this.pending.delete(element);
      element.classList.remove(PENDING_CSS);
    } else {
      for (const controller of this.controllers.values()) {
        controller.abort();
      }
      this.controllers.clear();
      for (const el of this.pending.keys()) {
        el.classList.remove(PENDING_CSS);
      }
      this.pending.clear();
      this.versions.clear();
    }
  }

  private makeKey(element: ValidatableElement, validatorName: string): string {
    return `${element.name}:${validatorName}`;
  }

  private addPending(element: ValidatableElement, validatorName: string): void {
    let set = this.pending.get(element);
    if (!set) {
      set = new Set();
      this.pending.set(element, set);
    }
    set.add(validatorName);
    element.classList.add(PENDING_CSS);
  }

  private removePending(element: ValidatableElement, validatorName: string): void {
    const set = this.pending.get(element);
    if (!set) {
      return;
    }

    set.delete(validatorName);
    if (set.size === 0) {
      this.pending.delete(element);
      element.classList.remove(PENDING_CSS);
    }
  }
}
