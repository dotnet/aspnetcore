import * as ng from 'angular2/angular2';
import * as models from '../../../models/models';

@ng.Component({
  selector: 'album-delete-prompt'
})
@ng.View({
  templateUrl: './ng-app/components/admin/album-delete-prompt/album-delete-prompt.html',
  directives: [ng.NgIf]
})
export class AlbumDeletePrompt {
    private modalElement: any;
    public album: models.Album;
    
    constructor(@ng.Inject(ng.ElementRef) elementRef: ng.ElementRef) {
        if (typeof window !== 'undefined') {
            this.modalElement = (<any>window).jQuery(".modal", elementRef.nativeElement);
        }
    }
    
    public show(album: models.Album) {
        this.album = album;
        this.modalElement.modal();
    }
}
