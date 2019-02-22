import { ApiAuthorizationModule } from './api-authorization.module';

describe('ApiAuthorizationModule', () => {
  let apiAuthorizationModule: ApiAuthorizationModule;

  beforeEach(() => {
    apiAuthorizationModule = new ApiAuthorizationModule();
  });

  it('should create an instance', () => {
    expect(apiAuthorizationModule).toBeTruthy();
  });
});
