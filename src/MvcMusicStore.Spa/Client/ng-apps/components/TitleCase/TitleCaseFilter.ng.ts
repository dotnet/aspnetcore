/// <reference path="..\..\MusicStore.TitleCase.ng.ts" />

module MusicStore.TitleCase {
    
    //@NgFilter('titlecase')
    function titleCase(input: string) {
        var out = "",
            lastChar = "";

        for (var i = 0; i < input.length; i++) {
            out = out + (lastChar === " " || lastChar === ""
                ? input.charAt(i).toUpperCase()
                : input.charAt(i));

            lastChar = input.charAt(i);
        }

        return out;
    }
    
    angular.module("MusicStore.TitleCase")
        .filter("titlecase", () => titleCase);
}