import * as React from 'react';
import { connect as nativeConnect } from 'react-redux';

export type ReactComponentClass<T, S> = new(props: T) => React.Component<T, S>;
export class ComponentBuilder<TOwnProps, TActions, TExternalProps> {
    constructor(private stateToProps: (appState: any) => TOwnProps, private actionCreators: TActions) {
    }

    public withExternalProps<TAddExternalProps>() {
        return this as any as ComponentBuilder<TOwnProps, TActions, TAddExternalProps>;
    }

    public get allProps(): TOwnProps & TActions & TExternalProps { return null; }

    public connect<TState>(componentClass: ReactComponentClass<TOwnProps & TActions & TExternalProps, TState>): ReactComponentClass<TExternalProps, TState> {
        return nativeConnect(this.stateToProps, this.actionCreators as any)(componentClass) as any;
    }
}

export function provide<TOwnProps, TActions>(stateToProps: (appState: any) => TOwnProps, actionCreators: TActions) {
    return new ComponentBuilder<TOwnProps, TActions, {}>(stateToProps, actionCreators);
}
