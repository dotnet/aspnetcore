module MusicStore.Truncate {

    //@NgFilter
    function truncate(input: string, length: number) {
        if (!input) {
            return input;
        }

        if (input.length <= length) {
            return input;
        } else {
            return input.substr(0, length).trim() + "…";
        }
    }
} 