import * as ng from '@angular/core';
import * as router from '@angular/router-deprecated';

@ng.Component({
  selector: 'nav-menu',
  template: require('./nav-menu.html'),
  directives: [router.ROUTER_DIRECTIVES]
})
export class NavMenu {
}
