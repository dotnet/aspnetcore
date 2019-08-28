import { ReconnectDisplay } from './ReconnectDisplay';
export class UserSpecifiedDisplay implements ReconnectDisplay {
  static readonly ShowClassName = 'components-reconnect-show';

  static readonly HideClassName = 'components-reconnect-hide';

  static readonly FailedClassName = 'components-reconnect-failed';

  static readonly RefusedClassName = 'components-reconnect-refused';

  constructor(private dialog: HTMLElement) {
  }

  show(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.ShowClassName);
  }

  hide(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.HideClassName);
  }

  failed(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.FailedClassName);
  }
  
  rejected(): void {
    this.removeClasses();
    this.dialog.classList.add(UserSpecifiedDisplay.RefusedClassName);
  }

  private removeClasses() {
    this.dialog.classList.remove(UserSpecifiedDisplay.ShowClassName, UserSpecifiedDisplay.HideClassName, UserSpecifiedDisplay.FailedClassName, UserSpecifiedDisplay.RefusedClassName);
  }
}
