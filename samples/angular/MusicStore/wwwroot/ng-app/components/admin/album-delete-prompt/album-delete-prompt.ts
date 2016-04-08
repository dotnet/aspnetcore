import * as ng from 'angular2/core';
import * as models from '../../../models/models';

@ng.Component({
  selector: 'album-delete-prompt',
  templateUrl: './ng-app/components/admin/album-delete-prompt/album-delete-prompt.html'
})
export class AlbumDeletePrompt {
    public album: models.Album;

    constructor(@ng.Inject(ng.ElementRef) private _elementRef: ng.ElementRef) {
    }

    public show(album: models.Album) {
        this.album = album;

        // Consider rewriting this using Angular 2's "Renderer" API so as to avoid direct DOM access
        (<any>window).jQuery(".modal", this._elementRef.nativeElement).modal();
    }
}
