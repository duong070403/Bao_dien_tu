// wwwroot/js/hover-dropdowns.js - Enhance dropdowns to activate on hover

$(function () {
    // Add additional animations when page loads
    setTimeout(function () {
        $('.navbar-brand img').addClass('animate__animated animate__pulse');
    }, 1500);

    // Remove Bootstrap's data attributes for click trigger on dropdowns
    $('.dropdown-toggle').each(function () {
        $(this).attr('data-bs-toggle', '');
        $(this).attr('aria-expanded', 'false');
    });

    // Add custom hover behavior
    $('.dropdown').on('mouseenter', function () {
        const $dropdown = $(this);
        const $menu = $dropdown.find('.dropdown-menu');

        $menu.addClass('animate__animated animate__fadeIn animate__faster');
        $dropdown.addClass('show');
        $dropdown.find('.dropdown-toggle').attr('aria-expanded', 'true');
        $menu.addClass('show');
    }).on('mouseleave', function () {
        const $dropdown = $(this);
        const $menu = $dropdown.find('.dropdown-menu');

        $dropdown.removeClass('show');
        $dropdown.find('.dropdown-toggle').attr('aria-expanded', 'false');
        $menu.removeClass('show');
    });

    // Add ripple effect to buttons
    $('.btn').on('mousedown', function (e) {
        const $btn = $(this);
        const x = e.pageX - $btn.offset().left;
        const y = e.pageY - $btn.offset().top;

        $btn.append('<span class="ripple"></span>');
        $btn.find('.ripple').css({
            left: x + 'px',
            top: y + 'px'
        }).animate({
            opacity: 0,
            width: '500px',
            height: '500px'
        }, 800, function () {
            $(this).remove();
        });
    });

    // Add extra animations for notification bell
    $('.notification-wrapper').on('mouseenter', function () {
        if (!$('.fas.fa-bell').hasClass('bell-animate')) {
            $('.fas.fa-bell').addClass('bell-animate');
            setTimeout(function () {
                $('.fas.fa-bell').removeClass('bell-animate');
            }, 1200);
        }
    });


    // Make navbar slightly smaller on scroll
    $(window).on('scroll', function () {
        if ($(this).scrollTop() > 100) {
            $('.navbar').css({
                'padding': '0.8rem 0',
                'box-shadow': '0 5px 15px rgba(0,0,0,0.1)'
            });
            $('.navbar-brand img').css('height', '45px');
        } else {
            $('.navbar').css({
                'padding': '1.2rem 0',
                'box-shadow': '0 2px 5px rgba(0,0,0,0.05)'
            });
            $('.navbar-brand img').css('height', '50px');
        }
    });
});
