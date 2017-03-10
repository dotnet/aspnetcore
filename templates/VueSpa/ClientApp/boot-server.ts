import { createServerRenderer, RenderResult } from 'aspnet-prerendering';
import { createBundleRenderer } from 'vue-server-renderer';
const path = require('path');
const bundleRenderer = createBundleRenderer(path.resolve('ClientApp/dist/vue-ssr-bundle.json'), {
    template: '<!--vue-ssr-outlet-->'
});

export default createServerRenderer(params => {
    return new Promise<RenderResult>((resolve, reject) => {
        bundleRenderer.renderToString(params, (error, html) => {
            if (error) {
                reject(error);
            } else {
                resolve({ html: html });
            }
        });
    });
});
