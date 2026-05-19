// This web component is used from the EventDuringBatchRendering test case

window.customElements.define('custom-web-component-performing-js-interop', class extends HTMLElement {
    connectedCallback() {
        this.attachShadow({ mode: 'open' });
        this.shadowRoot.innerHTML = `
            <div style='border: 2px dashed red; margin: 10px 0; padding: 5px; background: #dddddd;'>
                <slot></slot>
            </div>
        `;

        // Since this happens during batch rendering, it will be blocked.
        // In the future we could allow async calls, but this is enough of an edge case
        // that it doesn't need to be implemented currently. Developers who need to do this
        // can wrap their interop call in requestAnimationFrame or setTimeout(..., 0).
        (async function () {
            try {
                await DotNet.invokeMethodAsync('SomeAssembly', 'SomeMethodThatDoesntNeedToExistForThisTest');
            } catch (ex) {
                document.getElementById('web-component-error-log').innerText += ex.toString() + '\n';
            }
        })();
    }
});
