import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public fx?: Effects;
  public authenticated: boolean = false;
  public register: boolean = false;
  public operationFailed: boolean = false;
  public operationReason: string = "";

  public fetch() {
    this.operationFailed = false;
    this.http.get<Effects>(this.baseUrl + 'effects', { withCredentials: true }).subscribe(result => {
      this.fx = result;
      if (this.fx.username) {
        this.authenticated = true;
      }
      else {
        this.authenticated = false;
      }
    },
      error => {
        this.authenticated = false;
        this.operationFailed = true;
        this.operationReason = error;
      });
  }

  public toggleRegister() {
    this.register = this.register ? false : true;
                   
  };

  public validate(f: NgForm) {
    this.operationFailed = false;
    if (f.valid) {
      if (this.register) {
        if (f.value.password === f.value.pconfirm) {
          this.submitRegistration(f.value);
        }
        else {
          this.operationFailed = true;
          this.operationReason = "Passwords do not match";
        }
      }
      else {
        this.login(f.value);
      }
    }
    else {
      this.operationFailed = true;
      this.operationReason = "Form validation failed.";
    }
  }

  public submitRegistration(upwd: UserPwd) {
    this.operationFailed = false;
    this.http.post(this.baseUrl + 'identity/v1/register', {
      username: upwd.username, password: upwd.password
    }).subscribe(_ => {
        this.register = false;
        alert('You successfully registered. Now login!');
      }, error => {
        this.operationFailed = true;
        this.operationReason = error;
      });
  };

  public login(uwpd: UserPwd) {
    this.operationFailed = false;
    this.http.post(this.baseUrl + 'identity/v1/login', {
      username: uwpd.username, password: uwpd.password, cookieMode: true
    }, {
      withCredentials: true
    }).subscribe(_ => {
        this.authenticated = true;
        this.fetch();
      }, error => {
        this.authenticated = false;
        this.operationFailed = true;
        this.operationReason = error;
      });
  };

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) {
    http.get<Effects>(baseUrl + 'effects').subscribe(result => {
      this.fx = result;
      if (this.fx.username) {
        this.authenticated = true;
      }
    },
      error => console.error(error));
  }
}

interface Effects {
  username: string;
  effects: string[];
}

interface UserPwd {
  username: string;
  password: string;
}
