import * as ng from 'angular2/core';
import { NgIf } from 'angular2/common';
import * as models from '../../../models/models';

@ng.Component({
  selector: 'album-delete-prompt'
})
@ng.View({
  templateUrl: './ng-app/components/admin/album-delete-prompt/album-delete-prompt.html',
  directives: [NgIf]
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
