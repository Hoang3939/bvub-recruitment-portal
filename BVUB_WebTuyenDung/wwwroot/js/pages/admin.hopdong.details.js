// wwwroot/js/pages/admin.hopdong.details.js
(function () {
    const btnOpen = document.getElementById('btnDuyetHD');
    if (!btnOpen) return; // không hiển thị khi không pending / không phải admin

    const modal = document.getElementById('confirmApproveHD');
    const btnDo = document.getElementById('doApproveHD');
    const closes = modal ? modal.querySelectorAll('.modal-close') : [];

    const cfg = document.getElementById('admin-hd-details-config');
    const approveUrl = cfg?.dataset.approveUrl || '';
    const donId = cfg?.dataset.donId || '';
    const editUrl = cfg?.dataset.editUrl || '#';

    function openM() { if (modal) { modal.classList.add('show'); document.body.style.overflow = 'hidden'; } }
    function closeM() { if (modal) { modal.classList.remove('show'); document.body.style.overflow = ''; } }

    closes.forEach(x => x.addEventListener('click', closeM));
    if (modal) modal.addEventListener('click', e => { if (e.target === modal) closeM(); });
    btnOpen.addEventListener('click', openM);

    function token() {
        const t = document.querySelector('#af-hd input[name="__RequestVerificationToken"]');
        return t ? t.value : '';
    }

    btnDo?.addEventListener('click', async function () {
        try {
            const resp = await fetch(approveUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': token()
                },
                body: new URLSearchParams({ donType: 'HD', id: donId })
            });

            if (resp.status === 401 || resp.status === 403) { alert('Bạn không có quyền duyệt.'); return; }
            const data = await resp.json().catch(() => ({}));
            if (!data.ok) { alert(data.message || 'Duyệt thất bại'); return; }

            // cập nhật UI
            const badge = document.querySelector('.badge.pending');
            if (badge) { badge.textContent = 'Đã duyệt'; badge.classList.remove('pending'); badge.classList.add('approved'); }

            const a = document.createElement('a');
            a.className = 'btn btn-primary';
            a.href = editUrl; a.textContent = 'Sửa';
            btnOpen.replaceWith(a);

            closeM();
        } catch { alert('Lỗi mạng.'); }
    });
})();
