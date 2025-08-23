// wwwroot/js/pages/admin.guides.index.js
(function () {
    const cfg = document.getElementById('admin-guides-index-config') || {};
    const partialUrl = (cfg.dataset && cfg.dataset.partialUrl) || '';
    const editUrlTpl = (cfg.dataset && cfg.dataset.editUrlTpl) || '';
    const deleteUrl = (cfg.dataset && cfg.dataset.deleteUrl) || '';

    function openModal() { modal.classList.add('show'); document.body.style.overflow = 'hidden'; }
    function closeModal() { modal.classList.remove('show'); document.body.style.overflow = ''; }

    function getToken() {
        const f = document.querySelector('#af-guides input[name="__RequestVerificationToken"]');
        return f ? f.value : '';
    }

    const modal = document.getElementById('guideDetailModal');
    const bodyEl = document.getElementById('guideDetailContainer');
    const editEl = document.getElementById('guideEditBtn');
    const delEl = document.getElementById('guideDeleteBtn');
    const closeBtn = modal.querySelector('.modal-close');
    let currentId = null;

    closeBtn.addEventListener('click', closeModal);
    modal.addEventListener('click', e => { if (e.target === modal) closeModal(); });

    // Mở modal khi bấm card
    document.addEventListener('click', async function (e) {
        const card = e.target.closest('.rec-card');
        if (!card) return;

        currentId = card.getAttribute('data-id');
        bodyEl.innerHTML = 'Đang tải...';
        editEl.removeAttribute('href');

        try {
            const resp = await fetch(partialUrl + '?id=' + encodeURIComponent(currentId),
                { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            bodyEl.innerHTML = resp.ok ? await resp.text() : 'Không tải được dữ liệu.';
        } catch {
            bodyEl.innerHTML = 'Có lỗi khi tải dữ liệu.';
        }
        if (editUrlTpl) editEl.href = editUrlTpl.replace('__ID__', currentId);
        openModal();
    });

    // Xoá trong popup
    delEl.addEventListener('click', async function () {
        if (!currentId) return;
        if (!confirm('Xoá hướng dẫn này?')) return;

        try {
            const resp = await fetch(deleteUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': getToken()
                },
                body: new URLSearchParams({ id: currentId })
            });

            const data = await resp.json().catch(() => ({}));
            if (!resp.ok || !data.ok) {
                alert(data.message || 'Xoá thất bại.'); return;
            }

            // loại bỏ card khỏi danh sách
            const card = document.querySelector('.rec-card[data-id="' + currentId + '"]');
            if (card) card.remove();

            closeModal();
            alert('Đã xoá hướng dẫn.');
        } catch {
            alert('Có lỗi khi xoá.');
        }
    });
})();
