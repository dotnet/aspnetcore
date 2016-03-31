import * as ng from 'angular2/core';
import * as router from 'angular2/router';

@ng.Component({
  selector: 'nav-menu',
  template: require('./nav-menu.html'),
  directives: [router.ROUTER_DIRECTIVES]
})
export class NavMenu {
}
