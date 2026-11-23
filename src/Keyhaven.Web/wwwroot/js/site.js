(function () {
    const menuBtn = document.getElementById('menuBtn');
    const sidebar = document.getElementById('sidebar');
    const scrim = document.getElementById('sidebarScrim');

    function closeNav() {
        if (sidebar) sidebar.classList.remove('open');
        if (scrim) scrim.classList.remove('show');
    }

    if (menuBtn && sidebar) {
        menuBtn.addEventListener('click', function () {
            sidebar.classList.toggle('open');
            if (scrim) scrim.classList.toggle('show', sidebar.classList.contains('open'));
        });
    }
    if (scrim) scrim.addEventListener('click', closeNav);
    window.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeNav();
    });

    const toast = document.getElementById('toast');
    if (toast) {
        setTimeout(function () {
            toast.style.transition = 'opacity .4s, transform .4s';
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(8px)';
            setTimeout(function () { toast.remove(); }, 450);
        }, 6500);
    }

    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!window.confirm(form.getAttribute('data-confirm'))) {
                e.preventDefault();
            }
        });
    });

    document.querySelectorAll('[data-rent-from-property]').forEach(function (select) {
        const rentField = document.getElementById('RentAmount');
        if (!rentField) return;
        select.addEventListener('change', function () {
            const option = select.selectedOptions[0];
            if (!option) return;
            const match = option.text.match(/£([\d,]+)/);
            if (match) rentField.value = match[1].replace(/,/g, '');
        });
    });
})();
