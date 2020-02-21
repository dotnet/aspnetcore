import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
////#if (IndividualLocalAuth)
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
////#else
import { HttpClientModule } from '@angular/common/http';
////#endif
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { CounterComponent } from './counter/counter.component';
import { FetchDataComponent } from './fetch-data/fetch-data.component';
////#if (IndividualLocalAuth)
import { ApiAuthorizationModule } from 'src/api-authorization/api-authorization.module';
import { AuthorizeGuard } from 'src/api-authorization/authorize.guard';
import { AuthorizeInterceptor } from 'src/api-authorization/authorize.interceptor';
////#endif

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    CounterComponent,
    FetchDataComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    ////#if (IndividualLocalAuth)
    ApiAuthorizationModule,
    ////#endif
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'counter', component: CounterComponent },
      ////#if (IndividualLocalAuth)
      { path: 'fetch-data', component: FetchDataComponent, canActivate: [AuthorizeGuard] },
      ////#else
      { path: 'fetch-data', component: FetchDataComponent },
      ////#endif
    ])
  ],
  ////#if (IndividualLocalAuth)
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthorizeInterceptor, multi: true }
  ],
  ////#else
  providers: [],
  ////#endif
  bootstrap: [AppComponent]
})
export class AppModule { }
