import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { LoginMenuComponent } from './login-menu.component';
import { AuthorizeService } from '../authorize.service';
import { of } from 'rxjs';

describe('LoginMenuComponent', () => {
  let component: LoginMenuComponent;
  let fixture: ComponentFixture<LoginMenuComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      imports: [RouterTestingModule], 
      declarations: [ LoginMenuComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    let authService = TestBed.get(AuthorizeService);

    spyOn(authService, 'ensureUserManagerInitialized').and.returnValue(
      Promise.resolve());
    spyOn(authService, 'getUserFromStorage').and.returnValue(
      of(null));

    fixture = TestBed.createComponent(LoginMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
