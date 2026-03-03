// Web component that wraps an <input> element inside a Shadow DOM.
// Used to test that Blazor's event binding works correctly with Shadow DOM elements.
// Intentionally does NOT proxy the 'value' property so that the test
// exercises Blazor's composedPath() logic to reach the inner input.

window.customElements.define('shadow-dom-input', class extends HTMLElement {
    connectedCallback() {
        if (!this.shadowRoot) {
            const shadow = this.attachShadow({ mode: 'open' });
            this._input = document.createElement('input');
            this._input.type = 'text';
            shadow.appendChild(this._input);
        }
    }
});
