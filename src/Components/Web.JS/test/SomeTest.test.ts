import { expect, test } from '@jest/globals';
import * as br from '../src/Rendering/LogicalElements';

test('Jest test that should pass', () => {
    expect(1+2).toBe(3);

    const e = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    const le = br.toLogicalElement(e, true);
    expect(br.isSvgElement(le)).toBe(true);
});

test('Jest test that should fail', () => {
    expect(1+2).toBe(3);

    const e = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    const le = br.toLogicalElement(e, true);
    expect(br.isSvgElement(le)).toBe(false);
});
