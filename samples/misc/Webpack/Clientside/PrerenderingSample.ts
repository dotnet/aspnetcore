export default function (params: any): Promise<{ html: string, globals?: any }> {
    return new Promise((resolve, reject) => {

        // Here, you could put any logic that synchronously or asynchronously prerenders
        // your SPA components. For example, see the boot-server.ts files in the Angular2Spa
        // and ReactReduxSpa templates for ways to prerender Angular 2 and React components.
        //
        // If you wanted, you could use a property on the 'params.data' object to specify
        // which SPA component or template to render.

        const html = `
            <h1>Hello</h1>
            It works! You passed <b>${ JSON.stringify(params.data) }</b>
            and are currently requesting <b>${ params.location.path }</b>`;
        resolve({ html });
    });
};
