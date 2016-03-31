import * as ng from 'angular2/core';

@ng.Component({
  selector: 'counter',
  template: require('./counter.html')
})
export class Counter {
    public currentCount = 0;

    public incrementCounter() {
        this.currentCount++;
    }
}
