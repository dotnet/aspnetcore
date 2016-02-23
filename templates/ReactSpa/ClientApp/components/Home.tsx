import * as React from 'react';
import { carouselItems } from '../data/CarouselItems';
import { linkLists } from '../data/HomepageLinkLists';

export class Home extends React.Component<void, void> {
    public render() {
        return <div>
            { /* Carousel */ }
            <div id="myCarousel" className="carousel slide" data-ride="carousel" data-interval="6000">
                <ol className="carousel-indicators">
                    { carouselItems.map((item, index) =>
                        <li key={ index } data-target="#myCarousel" data-slide-to={ index } className={ index == 0 ? "active" : "" }></li>
                    )}
                </ol>
                <div className="carousel-inner" role="listbox">
                    { carouselItems.map((item, index) =>
                        <div key={ index } className={ "item " + (index == 0 ? "active" : "") }>
                            <img src={ item.imageUrl } alt={ item.imageAlt } className="img-responsive" />
                            <div className="carousel-caption">
                                <p>
                                    { item.text }
                                    <a className="btn btn-default" href={ item.learnMoreUrl }>Learn More</a>
                                </p>
                            </div>
                        </div>
                    )}
                </div>
                <a className="left carousel-control" href="#myCarousel" role="button" data-slide="prev">
                    <span className="glyphicon glyphicon-chevron-left" aria-hidden="true"></span>
                    <span className="sr-only">Previous</span>
                </a>
                <a className="right carousel-control" href="#myCarousel" role="button" data-slide="next">
                    <span className="glyphicon glyphicon-chevron-right" aria-hidden="true"></span>
                    <span className="sr-only">Next</span>
                </a>
            </div>

            { /* Lists of links */ }
            <div className="row">
                { linkLists.map((list, listIndex) =>
                    <div key={listIndex} className="col-md-3">
                        <h2>{ list.title }</h2>
                        <ul>
                            { list.entries.map((entry, entryIndex) => 
                                <li key={ entryIndex }>{ entry }</li>
                            )}
                        </ul>
                    </div>
                )}
            </div>
        </div>;
    }
}
