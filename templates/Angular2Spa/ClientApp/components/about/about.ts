import * as ng from 'angular2/core';

@ng.Component({
  selector: 'about'
})
@ng.View({
  template: require('./about.html')
})
export class About {
}
