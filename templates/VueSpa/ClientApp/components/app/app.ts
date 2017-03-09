import Vue from 'vue';
import { Component } from 'av-ts';

@Component({
    components: {
        MenuComponent: require('../navmenu/navmenu.vue.html')
    }
})
export default class AppComponent extends Vue {
}
