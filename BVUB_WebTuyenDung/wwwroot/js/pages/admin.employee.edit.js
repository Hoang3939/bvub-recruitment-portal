// wwwroot/js/pages/admin.employee.edit.js
(function () {
    const cfg = document.getElementById('admin-emp-edit-config') || {};
    const serverErr = (cfg.dataset && cfg.dataset.serverError) || '';
    const firstErr = (cfg.dataset && cfg.dataset.firstError) || '';

    function ensureToast() {
        let t = document.getElementById('niceToast');
        if (!t) {
            t = document.createElement('div');
            t.id = 'niceToast';
            t.className = 'toast-nice';
            t.innerHTML = '<span id="niceToastMsg">...</span><button type="button" class="close" aria-label="Đóng">&times;</button>';
            document.body.appendChild(t);
        }
        return t;
    }
    function niceToast(msg, type) {
        const t = ensureToast();
        t.classList.remove('success', 'error'); t.classList.add(type || 'success');
        t.querySelector('#niceToastMsg').textContent = msg || 'Thao tác thành công.';
        t.classList.add('show');
        const auto = setTimeout(() => t.classList.remove('show'), 2200);
        t.querySelector('.close').onclick = function () { clearTimeout(auto); t.classList.remove('show'); };
    }

    if (serverErr || firstErr) window.addEventListener('DOMContentLoaded', () => niceToast(serverErr || firstErr, 'error'));

    // Popup confirm admin password
    (function () {
        const form = document.getElementById('empEditForm');
        if (!form) return;
        const modal = document.getElementById('confirmPwdModal');
        const closes = modal.querySelectorAll('.modal-close');
        const input = document.getElementById('adminPwdInput');
        const hidden = document.getElementById('AdminPasswordHidden');
        const err = document.getElementById('adminPwdErr');

        function open() { modal.classList.add('show'); document.body.style.overflow = 'hidden'; input.value = ''; err.style.display = 'none'; input.focus(); }
        function close() { modal.classList.remove('show'); document.body.style.overflow = ''; }
        closes.forEach(x => x.addEventListener('click', close));
        modal.addEventListener('click', e => { if (e.target === modal) close(); });

        form.addEventListener('submit', function (e) {
            if (!hidden.value) { e.preventDefault(); open(); }
        });

        document.getElementById('btnConfirmSave').addEventListener('click', function () {
            if (!input.value) { err.style.display = 'block'; input.focus(); return; }
            hidden.value = input.value; close(); form.submit();
        });
    })();
})();
