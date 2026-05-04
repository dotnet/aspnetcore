// Web component that wraps an <input> element inside a Shadow DOM.
// Used to test that Blazor's event binding works correctly with Shadow DOM elements.

window.customElements.define('shadow-dom-input', class extends HTMLElement {
    connectedCallback() {
        const shadow = this.attachShadow({ mode: 'open' });
        this._input = document.createElement('input');
        this._input.type = 'text';
        shadow.appendChild(this._input);
    }

    get value() { return this._input?.value ?? ''; }
    set value(v) { if (this._input) this._input.value = v; }
});
