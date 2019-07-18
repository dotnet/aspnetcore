(global as any).DotNet = { attachReviver: jest.fn() };

import { discoverPrerenderedCircuits } from '../src/Platform/Circuits/CircuitManager';
import { JSDOM } from 'jsdom';

describe('CircuitManager', () => {

  it('discoverPrerenderedCircuits returns discovered prerendered circuits', () => {
    const dom = new JSDOM(`<!doctype HTML>
    <html>
      <head>
        <title>Page</title>
      </head>
      <body>
        <header>Preamble</header>
        <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":1} -->
        <p>Prerendered content</p>
        <!-- M.A.C.Component: 1 -->
        <footer></footer>
      </body>
    </html>`);

    const results = discoverPrerenderedCircuits(dom.window.document);

    expect(results.length).toEqual(1);
    expect(results[0].components.length).toEqual(1);
    const result = results[0].components[0];
    expect(result.circuitId).toEqual("1234");
    expect(result.rendererId).toEqual(2);
    expect(result.componentId).toEqual(1);

  });

  it('discoverPrerenderedCircuits returns discovers multiple prerendered circuits', () => {
    const dom = new JSDOM(`<!doctype HTML>
    <html>
      <head>
        <title>Page</title>
      </head>
      <body>
        <header>Preamble</header>
        <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":1} -->
        <p>Prerendered content</p>
        <!-- M.A.C.Component: 1 -->
        <footer>
          <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":2} -->
          <p>Prerendered content</p>
          <!-- M.A.C.Component: 2 -->
        </footer>
      </body>
    </html>`);

    const results = discoverPrerenderedCircuits(dom.window.document);

    expect(results.length).toEqual(1);
    expect(results[0].components.length).toEqual(2);
    const first = results[0].components[0];
    expect(first.circuitId).toEqual("1234");
    expect(first.rendererId).toEqual(2);
    expect(first.componentId).toEqual(1);

    const second = results[0].components[1];
    expect(second.circuitId).toEqual("1234");
    expect(second.rendererId).toEqual(2);
    expect(second.componentId).toEqual(2);
  });

  it('discoverPrerenderedCircuits throws for malformed circuits - improper nesting', () => {
    const dom = new JSDOM(`<!doctype HTML>
    <html>
      <head>
        <title>Page</title>
      </head>
      <body>
        <header>Preamble</header>
        <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":1} -->
        <p>Prerendered content</p>
        <!-- M.A.C.Component: 2 -->
        <footer>
        <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":2} -->
        <p>Prerendered content</p>
        <!-- M.A.C.Component: 1 -->
        </footer>
      </body>
    </html>`);

    expect(() => discoverPrerenderedCircuits(dom.window.document))
      .toThrow();
  });


  it('discoverPrerenderedCircuits throws for malformed circuits - mixed string and int', () => {
    const dom = new JSDOM(`<!doctype HTML>
    <html>
      <head>
        <title>Page</title>
      </head>
      <body>
        <header>Preamble</header>
        <!-- M.A.C.Component: {"circuitId":"1234","rendererId":"2","componentId":"1"} -->
        <p>Prerendered content</p>
        <!-- M.A.C.Component: 1 -->
        <footer>
        <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":2} -->
        <p>Prerendered content</p>
        <!-- M.A.C.Component: 2 -->
        </footer>
      </body>
    </html>`);

    expect(() => discoverPrerenderedCircuits(dom.window.document))
      .toThrow();
  });

  it('discoverPrerenderedCircuits initializes circuits', () => {
    const dom = new JSDOM(`<!doctype HTML>
    <html>
      <head>
        <title>Page</title>
      </head>
      <body>
        <header>Preamble</header>
        <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":1} -->
        <p>Prerendered content</p>
        <!-- M.A.C.Component: 1 -->
        <footer>
          <!-- M.A.C.Component: {"circuitId":"1234","rendererId":2,"componentId":2} -->
          <p>Prerendered content</p>
          <!-- M.A.C.Component: 2 -->
        </footer>
      </body>
    </html>`);

    const results = discoverPrerenderedCircuits(dom.window.document);

    for (let i = 0; i < results.length; i++) {
      const result = results[i];
      for (let j = 0; j < result.components.length; j++) {
        const component = result.components[j];
        component.initialize();
      }
    }

  });

});
