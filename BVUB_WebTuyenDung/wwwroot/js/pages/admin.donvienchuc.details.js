// wwwroot/js/pages/admin.donvienchuc.details.js
(function () {
    const cfgEl = document.getElementById('admin-vc-details-config');
    if (!cfgEl) return;

    const approveUrl = cfgEl.dataset.approveUrl || '';
    const editUrl = cfgEl.dataset.editUrl || '';
    const donId = cfgEl.dataset.donId || '';

    const btnOpen = document.getElementById('btnDuyetVC');
    const modal = document.getElementById('confirmApproveVC');
    const btnDo = document.getElementById('doApproveVC');

    if (!btnOpen || !modal || !btnDo) return;

    const closes = modal.querySelectorAll('.modal-close');
    function openM() { modal.classList.add('show'); document.body.style.overflow = 'hidden'; }
    function closeM() { modal.classList.remove('show'); document.body.style.overflow = ''; }

    closes.forEach(x => x.addEventListener('click', closeM));
    modal.addEventListener('click', e => { if (e.target === modal) closeM(); });
    btnOpen.addEventListener('click', openM);

    function token() {
        const t = document.querySelector('#af-vc input[name="__RequestVerificationToken"]');
        return t ? t.value : '';
    }

    btnDo.addEventListener('click', async function () {
        try {
            const resp = await fetch(approveUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': token()
                },
                body: new URLSearchParams({ donType: 'VC', id: donId })
            });
            if (resp.status === 401 || resp.status === 403) { alert('Bạn không có quyền duyệt.'); return; }
            const data = await resp.json().catch(() => null);
            if (!resp.ok || !data?.ok) { alert((data && (data.message || '')) || 'Duyệt thất bại'); return; }

            const badge = document.querySelector('.badge.pending');
            if (badge) { badge.textContent = data.newStatusLabel || 'Đã duyệt'; badge.classList.remove('pending'); badge.classList.add(data.newStatusClass || 'approved'); }

            const a = document.createElement('a'); a.className = 'btn btn-primary'; a.href = editUrl; a.textContent = 'Sửa';
            btnOpen.replaceWith(a);

            closeM();
        } catch {
            alert('Lỗi mạng.');
        }
    });
})();
