import Vue from 'vue';
import VueRouter from 'vue-router';

Vue.use(VueRouter);

export default new VueRouter({
    mode: 'history',
    routes: [
        { path: '/', component: require('./components/home/home.vue.html') },
        { path: '/counter', component: require('./components/counter/counter.vue.html') },
        { path: '/fetchdata', component: require('./components/fetchdata/fetchdata.vue.html') }
    ]
});
