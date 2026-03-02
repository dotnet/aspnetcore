// This web component is used from Shadow DOM input binding tests.
// It creates a shadow DOM with an input element inside, to test that
// Blazor's event handling uses composedPath() for shadow DOM events.

window.customElements.define('shadow-input', class extends HTMLElement {
    constructor() {
        super();
        this._shadowRoot = this.attachShadow({ mode: 'open' });
        this._input = document.createElement('input');
        this._input.setAttribute('type', 'text');
        this._shadowRoot.appendChild(this._input);
    }

    get value() {
        return this._input.value;
    }

    set value(val) {
        this._input.value = val;
    }
});
