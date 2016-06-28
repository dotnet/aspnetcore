import * as ng from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';

@ng.Component({
  selector: 'nav-menu',
  template: require('./nav-menu.html'),
  directives: [...ROUTER_DIRECTIVES]
})
export class NavMenu {
}
