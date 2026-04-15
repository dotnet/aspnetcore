import { expect, test, describe, jest } from '@jest/globals';
import { registerCustomEventType, getEventTypeOptions, getEventNameAliases } from '../src/Rendering/Events/EventTypes';

// The eventTypeRegistry is module-scoped and persists across tests, so we need
// unique event names per test to avoid interference from built-in registrations
// and other tests.

describe('registerCustomEventType duplicate registration', () => {
  test('duplicate registration with same browserEventName is silently ignored', () => {
    const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

    const options1 = {
      browserEventName: 'change',
      createEventArgs: (e: Event) => ({ checked: true }),
    };

    const options2 = {
      browserEventName: 'change',
      createEventArgs: (e: Event) => ({ checked: false, extra: 'data' }),
    };

    // First registration succeeds
    registerCustomEventType('mycheckedchange', options1);

    // Second registration with same browserEventName — should NOT throw
    registerCustomEventType('mycheckedchange', options2);

    // Should have warned
    expect(warnSpy).toHaveBeenCalledWith(
      expect.stringContaining('mycheckedchange')
    );

    // First registration wins (the registry entry is not overwritten)
    const registered = getEventTypeOptions('mycheckedchange');
    expect(registered).toBe(options1);

    // The alias array should only have one entry, not two
    const aliases = getEventNameAliases('change');
    const count = aliases!.filter(a => a === 'mycheckedchange').length;
    expect(count).toBe(1);

    warnSpy.mockRestore();
  });

  test('duplicate registration with different browserEventName is silently ignored', () => {
    const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

    registerCustomEventType('myevent-diffbrowser', {
      browserEventName: 'click',
      createEventArgs: (e: Event) => ({}),
    });

    // Different browserEventName — still should NOT throw, just warn
    registerCustomEventType('myevent-diffbrowser', {
      browserEventName: 'mouseover',
      createEventArgs: (e: Event) => ({}),
    });

    expect(warnSpy).toHaveBeenCalled();

    // First registration wins
    const registered = getEventTypeOptions('myevent-diffbrowser');
    expect(registered!.browserEventName).toBe('click');

    warnSpy.mockRestore();
  });

  test('duplicate registration does not cause double alias entries', () => {
    const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

    registerCustomEventType('mytabchange', {
      browserEventName: 'change',
      createEventArgs: (e: Event) => ({ activeId: 'tab1' }),
    });

    // Register again — simulates JS initializer re-firing
    registerCustomEventType('mytabchange', {
      browserEventName: 'change',
      createEventArgs: (e: Event) => ({ activeId: 'tab2' }),
    });

    // Register a third time
    registerCustomEventType('mytabchange', {
      browserEventName: 'change',
      createEventArgs: (e: Event) => ({ activeId: 'tab3' }),
    });

    // The alias for 'change' should only contain 'mytabchange' once
    const aliases = getEventNameAliases('change');
    const count = aliases!.filter(a => a === 'mytabchange').length;
    expect(count).toBe(1);

    warnSpy.mockRestore();
  });

  test('simulates FluentUI re-initialization scenario', () => {
    // This simulates what happens when FluentUI's afterStarted hook fires
    // multiple times (e.g., after enhanced navigation or circuit reconnection).
    // FluentUI registers ~18 custom events. On re-initialization, it tries
    // to register them all again.

    const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

    const fluentEvents = [
      { name: 'flu-checkedchange', browserEventName: 'change' },
      { name: 'flu-switchcheckedchange', browserEventName: 'change' },
      { name: 'flu-sliderchange', browserEventName: 'change' },
      { name: 'flu-accordionchange', browserEventName: 'change' },
      { name: 'flu-tabchange', browserEventName: 'change' },
    ];

    // First initialization — all should register successfully
    for (const evt of fluentEvents) {
      registerCustomEventType(evt.name, {
        browserEventName: evt.browserEventName,
        createEventArgs: (e: Event) => ({}),
      });
    }

    expect(warnSpy).not.toHaveBeenCalled();

    // Second initialization (simulates enhanced nav / circuit reconnect)
    // — should NOT throw, just warn
    for (const evt of fluentEvents) {
      registerCustomEventType(evt.name, {
        browserEventName: evt.browserEventName,
        createEventArgs: (e: Event) => ({}),
      });
    }

    // Should have warned for each duplicate
    expect(warnSpy).toHaveBeenCalledTimes(fluentEvents.length);

    warnSpy.mockRestore();
  });
});
