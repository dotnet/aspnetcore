import './css/site.css';
import Vue from 'vue';
import router from './router';

const App = require('./components/app/app.vue.html');

new Vue({
    el: 'app',
    render: h => h(App, { props: {} }),
    router: router
});
