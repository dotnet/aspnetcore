import Vue from 'vue';
import VueRouter from 'vue-router';
import { routes } from './routes';
Vue.use(VueRouter);

new Vue({
    el: '#app-root',
    router: new VueRouter({ mode: 'history', routes: routes }),
    render: h => h(require('./components/app/app.vue.html'))
});
