// Custom validation providers for the BlazorSSR sample app.
// This file is discovered automatically by Blazor via the {AssemblyName}.lib.module.js convention.

export function afterWebStarted(blazor) {
    // 'noprofanity' provider — matches the NoProfanityAttribute (C#).
    blazor.validation.addProvider('noprofanity', function (value, element, params) {
        if (!value) return true;
        var words = (params.words || '').split(',');
        for (var i = 0; i < words.length; i++) {
            var word = words[i].trim().toLowerCase();
            if (word && value.toLowerCase().indexOf(word) >= 0) {
                return false;
            }
        }
        return true;
    });
}
