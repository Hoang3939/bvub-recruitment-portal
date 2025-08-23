// wwwroot/js/pages/admin.positions.index.js
(function () {
    // ===== Helpers modal =====
    function openModal(el) { el?.classList.add('show'); document.body.style.overflow = 'hidden'; }
    function closeModal(el) { el?.classList.remove('show'); document.body.style.overflow = ''; }

    // ===== Modal chi tiết =====
    const detailModal = document.getElementById('posDetailModal');
    const detailBody = document.getElementById('posDetailContainer');
    const closeEls = detailModal ? detailModal.querySelectorAll('.modal-close') : [];
    closeEls.forEach(el => el.addEventListener('click', () => closeModal(detailModal)));
    detailModal?.addEventListener('click', e => { if (e.target === detailModal) closeModal(detailModal); });

    function isInteractive(target) {
        return !!target.closest('button, a, .btn, .btn-icon, input, select, textarea, svg, path');
    }

    const cfg = document.getElementById('pos-index-config');
    const detailsUrl = cfg?.dataset.detailsUrl || '';

    document.addEventListener('click', async function (e) {
        if (isInteractive(e.target)) return;

        const row = e.target.closest('tr.row-click[data-id]');
        if (!row) return;

        const id = row.getAttribute('data-id');
        if (!id) return;

        if (detailBody) detailBody.textContent = 'Đang tải...';
        try {
            const resp = await fetch(detailsUrl + '?id=' + encodeURIComponent(id), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const html = resp.ok ? await resp.text() : 'Không tải được dữ liệu.';
            if (detailBody) detailBody.innerHTML = html;
        } catch {
            if (detailBody) detailBody.textContent = 'Có lỗi khi tải dữ liệu.';
        }
        openModal(detailModal);
    });

    // ===== Confirm Delete =====
    const delModal = document.getElementById('confirmDelete');
    const delClose = delModal ? delModal.querySelectorAll('.modal-close') : [];
    const btnDelNo = document.getElementById('btnDelCancel');
    const btnDelYes = document.getElementById('btnDelYes');
    let targetFormSel = null;

    function openDel() { openModal(delModal); }
    function closeDel() { closeModal(delModal); targetFormSel = null; }

    delClose.forEach(el => el.addEventListener('click', closeDel));
    btnDelNo?.addEventListener('click', closeDel);
    delModal?.addEventListener('click', e => { if (e.target === delModal) closeDel(); });

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-delete');
        if (!btn) return;
        targetFormSel = btn.getAttribute('data-form');
        openDel();
    });

    btnDelYes?.addEventListener('click', function () {
        if (!targetFormSel) return closeDel();
        const frm = document.querySelector(targetFormSel);
        if (frm) frm.submit(); // đã có AntiForgery trong từng form
        closeDel();
    });
})();
