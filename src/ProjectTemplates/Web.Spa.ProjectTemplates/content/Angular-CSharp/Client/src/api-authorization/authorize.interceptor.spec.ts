import { TestBed, inject } from '@angular/core/testing';

import { AuthorizeInterceptor } from './authorize.interceptor';

describe('AuthorizeInterceptor', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthorizeInterceptor]
    });
  });

  it('should be created', inject([AuthorizeInterceptor], (service: AuthorizeInterceptor) => {
    expect(service).toBeTruthy();
  }));
});
