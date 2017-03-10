import Vue from 'vue';
import VueRouter from 'vue-router';
import { createServerRenderer, RenderResult } from 'aspnet-prerendering';
import { createBundleRenderer } from 'vue-server-renderer';
import { routes } from './routes';
Vue.use(VueRouter);

export default function(context: any) {
    const router = new VueRouter({ mode: 'history', routes: routes })
    router.push(context.url);

    return new Vue({
        render: h => h(require('./components/app/app.vue.html')),
        router: router
    });
}
