import { TestBed, inject } from '@angular/core/testing';

import { AuthorizeGuard } from './authorize.guard';

describe('AuthorizeGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthorizeGuard]
    });
  });

  it('should ...', inject([AuthorizeGuard], (guard: AuthorizeGuard) => {
    expect(guard).toBeTruthy();
  }));
});
