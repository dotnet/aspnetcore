import * as ng from 'angular2/core';

@ng.Component({
  selector: 'counter'
})
@ng.View({
  template: require('./counter.html')
})
export class Counter {
    public currentCount = 0;
    
    public incrementCounter() {
        this.currentCount++;
    }
}
