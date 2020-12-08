import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { LoginComponent } from './login.component';
import { ActivatedRoute, ActivatedRouteSnapshot, UrlSegment, convertToParamMap, Params, Router } from '@angular/router';
import { of } from 'rxjs';
import { LoginActions } from '../api-authorization.constants';
import { HttpParams } from '@angular/common/http';
import { AuthorizeService } from '../authorize.service';
import { log } from 'util';
import { HomeComponent } from 'src/app/home/home.component';

class RouterStub {
  url = '';
  navigate(commands: any[], extras?: any) {}
}

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let router: Router;

  beforeEach(async(() => {
    log('login.component.spec.ts before each started...');

    let tempParams: Params = { id: '1234' };

    let segment0: UrlSegment = new UrlSegment('segment0', {});
    let segment1: UrlSegment = new UrlSegment(LoginActions.Login, {});

    let urlSegments: UrlSegment[] = [segment0, segment1];

    TestBed.configureTestingModule({
      imports: [
        RouterTestingModule.withRoutes([
        { path: 'authentication/login-failed', component: HomeComponent }
      ])],
      declarations: [LoginComponent, HomeComponent],
      providers: [{
        provide: ActivatedRoute, useValue: {
          snapshot: {
            paramMap: convertToParamMap(tempParams),
            url: urlSegments,
            queryParams: tempParams
          }
        }
      }]
    }).compileComponents();

    router = TestBed.get(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));

    log('login.component.spec.ts before each after compileComponents()...');
  }));

  beforeEach(() => {
    let authService = TestBed.get(AuthorizeService);

    spyOn(authService, 'ensureUserManagerInitialized').and.returnValue(
      Promise.resolve());

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
