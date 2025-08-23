(function () {
    const cfg = document.getElementById('rec-index-config');
    const isAdmin = (cfg?.dataset.isAdmin === 'true');
    const detailsUrl = cfg?.dataset.detailsUrl || '';
    const editTpl = cfg?.dataset.editUrlTemplate || '';
    const deleteUrl = cfg?.dataset.deleteUrl || '';

    const modal = document.getElementById('recDetailModal');
    const bodyEl = document.getElementById('recDetailContainer');
    const editBtn = document.getElementById('recEditBtn');
    const delBtn = document.getElementById('recDeleteBtn');
    let currentId = null;

    function openModal() { modal.classList.add('show'); document.body.style.overflow = 'hidden'; }
    function closeModal() { modal.classList.remove('show'); document.body.style.overflow = ''; }

    modal.querySelectorAll('.modal-close').forEach(x => x.addEventListener('click', closeModal));
    modal.addEventListener('click', e => { if (e.target === modal) closeModal(); });

    // Ẩn nút sửa/xoá nếu không phải admin
    if (!isAdmin) {
        editBtn?.classList.add('d-none');
        delBtn?.classList.add('d-none');
    }

    function isInteractive(t) {
        return !!t.closest('a,button,.btn,.btn-icon,input,select,textarea,svg,path');
    }

    // click card -> open modal
    document.addEventListener('click', async function (e) {
        const card = e.target.closest('.rec-card');
        if (!card || isInteractive(e.target)) return;

        currentId = card.getAttribute('data-id');
        if (!currentId) return;

        bodyEl.innerHTML = 'Đang tải...';
        try {
            const resp = await fetch(detailsUrl + '?id=' + encodeURIComponent(currentId), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            bodyEl.innerHTML = resp.ok ? await resp.text() : 'Không tải được dữ liệu.';
        } catch {
            bodyEl.innerHTML = 'Có lỗi khi tải dữ liệu.';
        }

        if (isAdmin && editBtn) editBtn.href = editTpl.replace('__ID__', currentId);
        openModal();
    });

    // Xóa
    delBtn?.addEventListener('click', async function () {
        if (!isAdmin || !currentId) return;
        if (!confirm('Xóa thông tin tuyển dụng này? Hành động không thể hoàn tác.')) return;

        const tokenEl = document.querySelector('#af-rec input[name="__RequestVerificationToken"]');
        const token = tokenEl ? tokenEl.value : '';

        try {
            const resp = await fetch(deleteUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': token
                },
                body: new URLSearchParams({ id: currentId })
            });
            const data = await resp.json().catch(() => ({}));
            if (!resp.ok || !data.ok) { alert(data.message || 'Xóa thất bại.'); return; }

            const card = document.querySelector('.rec-card[data-id="' + currentId + '"]');
            if (card) card.remove();
            closeModal();
            alert('Đã xóa thông tin tuyển dụng.');
        } catch {
            alert('Có lỗi khi xóa.');
        }
    });
})();
