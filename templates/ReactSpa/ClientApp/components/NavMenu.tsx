import * as React from 'react';
import { Link } from 'react-router';
export class NavMenu extends React.Component<void, void> {
    public render() {
        return <div className="navbar navbar-inverse navbar-fixed-top">
            <div className="container">
                <div className="navbar-header">
                    <button type="button" className="navbar-toggle" data-toggle="collapse" data-target=".navbar-collapse">
                        <span className="sr-only">Toggle navigation</span>
                        <span className="icon-bar"></span>
                        <span className="icon-bar"></span>
                        <span className="icon-bar"></span>
                    </button>
                    <Link className="navbar-brand" to={ '/' }>WebApplicationBasic</Link>
                </div>
                <div className="navbar-collapse collapse">
                    <ul className="nav navbar-nav">
                        <li><Link to={ '/' }>Home</Link></li>
                        <li><Link to={ '/about' }>About</Link></li>
                        <li><Link to={ '/counter' }>Counter</Link></li>
                    </ul>
                </div>
            </div>
        </div>;
    }
}
