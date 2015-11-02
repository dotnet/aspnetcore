import * as ng from 'angular2/angular2';
import * as router from 'angular2/router';
import { AlbumsList } from '../albums-list/albums-list';
import { AlbumDetails } from '../album-details/album-details';
import { AlbumEdit } from '../album-edit/album-edit';

@ng.Component({
  selector: 'admin-home'
})
@router.RouteConfig([
  { path: 'albums', as: 'Albums', component: AlbumsList },
  { path: 'album/details/:albumId', as: 'AlbumDetails', component: AlbumDetails },
  { path: 'album/edit/:albumId', as: 'AlbumEdit', component: AlbumEdit }
])
@ng.View({
  templateUrl: './ng-app/components/admin/admin-home/admin-home.html',
  directives: [router.ROUTER_DIRECTIVES]
})
export class AdminHome {
    
}
