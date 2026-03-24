import { expect, test, describe } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

describe('Virtualize exports', () => {
  test('exports expected functions', () => {
    expect(typeof Virtualize.init).toBe('function');
    expect(typeof Virtualize.dispose).toBe('function');
    expect(typeof Virtualize.scrollToBottom).toBe('function');
    expect(typeof Virtualize.refreshObservers).toBe('function');
  });
});
