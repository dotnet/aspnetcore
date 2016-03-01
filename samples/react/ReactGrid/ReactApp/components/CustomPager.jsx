import * as React from 'react';
import { Link } from 'react-router';

export class CustomPager extends React.Component {
    pageChange(event) {
        this.props.setPage(parseInt(event.target.getAttribute("data-value")));
    }

    render() {
        var previous = null;
        var next = null;

        if(this.props.currentPage > 0){
            previous = <div className="btn btn-default"><Link className="previous" to={'/' + (this.props.currentPage)}><i className="glyphicon glyphicon-arrow-left"></i>{this.props.previousText}</Link></div>;
        }

        if(this.props.currentPage != (this.props.maxPage -1)){
            next = <div className="btn btn-default"><Link className="next" to={'/' + (this.props.currentPage + 2)}><i className="glyphicon glyphicon-arrow-right"></i>{this.props.nextText}</Link></div>;
        }

        var options = [];

        var startIndex = Math.max(this.props.currentPage - 5, 0);
        var endIndex = Math.min(startIndex + 11, this.props.maxPage);

        if (this.props.maxPage >= 11 && (endIndex - startIndex) <= 10) {
            startIndex = endIndex - 11;
        }

        for(var i = startIndex; i < endIndex ; i++){
            var selected = this.props.currentPage == i ? "btn-default" : "";
            options.push(<div key={i} className={"btn " + selected}><Link to={'/' + (i+1)}>{i+1}</Link></div>);
        }

        return (
            <div className="btn-group">
                {previous}
                {options}
                {next}
            </div>
        );
    }
}

CustomPager.defaultProps = {
    maxPage: 0,
    nextText: '',
    previousText: '',
    currentPage: 0
};
