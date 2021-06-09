import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { LogoutComponent } from './logout.component';
import { HomeComponent } from 'src/app/home/home.component';
import { ActivatedRoute, convertToParamMap, Params, UrlSegment } from '@angular/router';
import { LogoutActions } from '../api-authorization.constants';

describe('LogoutComponent', () => {
  let component: LogoutComponent;
  let fixture: ComponentFixture<LogoutComponent>;

  beforeEach(async(() => {
    let tempParams: Params = { id: '1234' };

    let segment0: UrlSegment = new UrlSegment('segment0', {});
    let segment1: UrlSegment = new UrlSegment(LogoutActions.LoggedOut, {});

    let urlSegments: UrlSegment[] = [segment0, segment1];

    TestBed.configureTestingModule({
      imports: [
        RouterTestingModule.withRoutes([
        { path: 'authentication/login-failed', component: HomeComponent }
      ])],
      declarations: [LogoutComponent, HomeComponent],
      providers: [{
        provide: ActivatedRoute, useValue: {
          snapshot: {
            paramMap: convertToParamMap(tempParams),
            url: urlSegments,
            queryParams: tempParams
          }
        }
      }]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LogoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
