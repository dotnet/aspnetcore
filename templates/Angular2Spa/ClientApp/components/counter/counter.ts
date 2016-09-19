import { Component } from '@angular/core';

@Component({
  selector: 'counter',
  template: require('./counter.html')
})
export class Counter {
    public currentCount = 0;

    public incrementCounter() {
        this.currentCount++;
    }
}
