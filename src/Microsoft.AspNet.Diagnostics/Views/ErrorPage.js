
(function ($) {
    $('.collapsable').hide();
    $('.page').hide();
    $('#stackpage').show();

    $('.frame').click(function () {
        $(this).children('.source').children('.collapsable').toggle('fast');
    });

    $('.frame').keypress(function (e) {
        if (e.which == 13) {
            $(this).children('.source').children('.collapsable').toggle('fast');
        }
    });
    
    $('#header li').click(function () {

        var unselected = $('#header .selected').removeClass('selected').attr('id');
        var selected = $(this).addClass('selected').attr('id');
        
        $('#' + unselected + 'page').hide();
        $('#' + selected + 'page').show('fast');
    });

    $('#header li').keypress(function (e) {
        if (e.which == 13) {
            var unselected = $('#header .selected').removeClass('selected').attr('id');
            var selected = $(this).addClass('selected').attr('id');

            $('#' + unselected + 'page').hide();
            $('#' + selected + 'page').show('fast');
        }
    });
    
})(jQuery);
