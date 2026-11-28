// Replaces the global FinalizationRegistry before Microsoft.JSInterop is imported,
// so a test can run the cleanup callback for a target without a real garbage collection.

interface Registration {
  registry: FakeFinalizationRegistry;
  target: object;
  heldValue: any;
  unregisterToken?: object;
}

const registrations: Registration[] = [];

class FakeFinalizationRegistry {
  // eslint-disable-next-line no-empty-function
  constructor(private readonly cleanup: (heldValue: any) => void) {
  }

  register(target: object, heldValue: any, unregisterToken?: object): void {
    registrations.push({ registry: this, target, heldValue, unregisterToken });
  }

  unregister(unregisterToken: object): boolean {
    const before = registrations.length;
    for (let i = registrations.length - 1; i >= 0; i--) {
      if (registrations[i].registry === this && registrations[i].unregisterToken === unregisterToken) {
        registrations.splice(i, 1);
      }
    }
    return registrations.length !== before;
  }

  runCleanup(heldValue: any): void {
    this.cleanup(heldValue);
  }
}

(globalThis as any).FinalizationRegistry = FakeFinalizationRegistry;

// Acts as if the browser collected the target: runs the cleanup callback of every registration for it.
export function collect(target: object): void {
  for (let i = registrations.length - 1; i >= 0; i--) {
    const registration = registrations[i];
    if (registration.target === target) {
      registrations.splice(i, 1);
      registration.registry.runCleanup(registration.heldValue);
    }
  }
}
