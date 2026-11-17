window.registerCustomValidator = function () {
    Blazor.formValidation.addValidator('startswith', function (context) {
        if (!context.value) {
            return { success: true };
        }

        const prefix = context.params['prefix'] || '';
        return { success: context.value.indexOf(prefix) === 0 };
    });
};
