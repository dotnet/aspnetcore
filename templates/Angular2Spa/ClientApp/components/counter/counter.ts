import * as ng from '@angular/core';

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
