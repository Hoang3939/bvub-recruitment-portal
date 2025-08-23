// wwwroot/js/pages/admin.departments.index.js
(function () {
    function cfg() {
        const el = document.getElementById('admin-dept-index-config');
        return { detailsUrl: el ? (el.dataset.urlDetails || '') : '' };
    }

    function modalApi(id) {
        const root = document.getElementById(id);
        const closeEls = root ? root.querySelectorAll('.modal-close') : [];
        const api = {
            open() { if (root) { root.classList.add('show'); document.body.style.overflow = 'hidden'; } },
            close() { if (root) { root.classList.remove('show'); document.body.style.overflow = ''; } },
            root
        };
        closeEls.forEach(x => x.addEventListener('click', api.close));
        root && root.addEventListener('click', e => { if (e.target === root) api.close(); });
        return api;
    }

    function showToastIfAny() {
        const t = document.getElementById('toast');
        if (!t) return;
        setTimeout(() => {
            t.classList.add('show');
            setTimeout(() => t.classList.remove('show'), 2300);
        }, 60);
    }

    document.addEventListener('DOMContentLoaded', function () {
        const C = cfg();

        // Details modal
        const detailM = modalApi('deptDetailModal');
        const detailBox = document.getElementById('deptDetailContainer');

        document.addEventListener('click', async e => {
            // bỏ qua click vào nút/link/icon/inputs
            const inter = e.target.closest('button, a, .btn-icon, svg, path, input, select');
            if (inter) return;

            const row = e.target.closest('tr.row-click[data-id]');
            if (!row) return;

            const id = row.getAttribute('data-id');
            if (!id) return;

            if (detailBox) detailBox.innerHTML = 'Đang tải...';
            try {
                const url = C.detailsUrl + '?id=' + encodeURIComponent(id);
                const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                detailBox.innerHTML = resp.ok ? await resp.text() : 'Không tải được dữ liệu.';
            } catch {
                detailBox.innerHTML = 'Có lỗi khi tải dữ liệu.';
            }
            detailM.open();
        });

        // Confirm delete
        const delM = modalApi('confirmDelete');
        const btnDelNo = document.getElementById('btnDelCancel');
        const btnDelYes = document.getElementById('btnDelYes');
        let targetFormSel = null;

        document.addEventListener('click', e => {
            const btn = e.target.closest('.btn-delete');
            if (!btn) return;
            targetFormSel = btn.getAttribute('data-form');
            delM.open();
        });

        btnDelNo && btnDelNo.addEventListener('click', delM.close);
        btnDelYes && btnDelYes.addEventListener('click', () => {
            if (!targetFormSel) return delM.close();
            const frm = document.querySelector(targetFormSel);
            if (frm) frm.submit();
            delM.close();
        });

        // Toast
        showToastIfAny();
    });
})();
