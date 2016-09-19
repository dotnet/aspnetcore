import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { UniversalModule } from 'angular2-universal';

import { 
  App,
  Counter,
  FetchData,
  Home,
  NavMenu
} from './components';

import { routes } from './routes';

/* NOTE :

  This file and `main.browser.ts` are identical, at the moment(!)
  By splitting these, you're able to create logic, imports, etc
          that are "Platform" specific.

  If you want your code to be completely Universal and don't need that
  You can also just have 1 file, that is imported into both
    * boot-client
    * boot-server 
    
*/
  
// ** Top-level NgModule "container" **
@NgModule({

  // Root App Component
  bootstrap: [ App ],

  // Our Components
  declarations: [
    App, Counter, FetchData, Home, NavMenu 
  ],

  imports: [
    
    // * NOTE: Needs to be your first import (!)
    UniversalModule, 
    // ^ NodeModule, NodeHttpModule, NodeJsonpModule are included for server

    // Your other imports can go here:
    FormsModule,
    
    // App Routing
    RouterModule.forRoot(routes)
  ]
})
export class MainModule {

}
