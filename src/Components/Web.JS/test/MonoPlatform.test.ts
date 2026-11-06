// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { beforeEach, describe, expect, jest, test } from '@jest/globals';

// The runtime package is supplied by the runtime build, not by npm, so jest cannot resolve it.
jest.mock('@microsoft/dotnet-runtime', () => ({
  GlobalizationMode: { Single: 'single', Sharded: 'sharded', All: 'all' },
}), { virtual: true });

describe('MonoPlatform setProgress', () => {
  let setPropertyCalls: [string, string][];
  let setProgress: (resourcesLoaded: number, totalResources: number) => void;

  beforeEach(async () => {
    jest.resetModules();
    setPropertyCalls = [];
    jest.spyOn(document.documentElement.style, 'setProperty').mockImplementation((name: string, value: string) => {
      setPropertyCalls.push([name, value]);
    });
    ({ setProgress } = await import('../src/Platform/Mono/MonoPlatform'));
  });

  function displayedPercentages(): number[] {
    return setPropertyCalls
      .filter(([name]) => name === '--blazor-load-percentage')
      .map(([, value]) => parseFloat(value));
  }

  function displayedPercentageTexts(): string[] {
    return setPropertyCalls
      .filter(([name]) => name === '--blazor-load-percentage-text')
      .map(([, value]) => value);
  }

  test('never decreases and never reaches 100 when the total grows during loading', () => {
    setProgress(25, 50);
    setProgress(50, 50);
    setProgress(50, 51);
    setProgress(51, 51);

    expect(displayedPercentageTexts()).toEqual(['"50%"', '"99%"', '"99%"', '"99%"']);

    const percentages = displayedPercentages();
    for (let i = 1; i < percentages.length; i++) {
      expect(percentages[i]).toBeGreaterThanOrEqual(percentages[i - 1]);
    }
    expect(Math.max(...percentages)).toBeLessThan(100);
  });

  test('reports ordinary progress while the total is stable', () => {
    setProgress(1, 4);
    setProgress(2, 4);
    setProgress(3, 4);

    expect(displayedPercentageTexts()).toEqual(['"25%"', '"50%"', '"75%"']);
  });

  test('ignores calls with no resources', () => {
    setProgress(0, 0);

    expect(setPropertyCalls).toEqual([]);
  });
});
