import Vue from 'vue';
import { Component } from 'av-ts';

@Component
export default class CounterComponent extends Vue {
    currentcount: number = 0;

    incrementCounter() {
        this.currentcount++;
    }
}
