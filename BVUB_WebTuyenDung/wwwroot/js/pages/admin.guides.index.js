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

    // Xoá trong popup (đẹp + toast)
    delEl.addEventListener('click', function () {
        if (!currentId) return;

        // --- Modal xác nhận ---
        const overlay = document.createElement('div');
        overlay.className = 'modal-overlay show';
        overlay.innerHTML = `
        <div class="modal-card" style="max-width:400px;">
            <div class="modal-header">
                <h3 class="modal-title">Xác nhận xóa</h3>
                <button class="modal-close" aria-label="Đóng">&times;</button>
            </div>
            <div class="modal-body" style="font-size:15px;line-height:1.5;">
                Bạn có chắc chắn muốn xóa hướng dẫn này?<br>
                <b>Hành động này không thể hoàn tác.</b>
            </div>
            <div class="modal-footer">
                <button class="btn btn-clear btn-cancel">Hủy</button>
                <button class="btn btn-danger btn-ok">Xóa</button>
            </div>
        </div>
    `;
        document.body.appendChild(overlay);

        // Đóng modal
        const closeConfirm = () => overlay.remove();
        overlay.querySelector('.modal-close').addEventListener('click', closeConfirm);
        overlay.querySelector('.btn-cancel').addEventListener('click', closeConfirm);
        overlay.addEventListener('click', e => { if (e.target === overlay) closeConfirm(); });

        // Khi nhấn "Xóa"
        overlay.querySelector('.btn-ok').addEventListener('click', async () => {
            const token = getToken();
            overlay.querySelector('.btn-ok').disabled = true;

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
                if (!resp.ok || !data.ok) {
                    showToast(data.message || 'Xóa thất bại.', 'error');
                    return;
                }

                // Xóa thành công
                closeConfirm();
                closeModal();

                const card = document.querySelector(`.rec-card[data-id="${currentId}"]`);
                if (card) card.remove();

                showToast('Đã xóa hướng dẫn.', 'success');
            } catch {
                showToast('Có lỗi khi xóa.', 'error');
            } finally {
                overlay.remove();
            }
        });
    });

    document.addEventListener('DOMContentLoaded', () => {
        const holder = document.getElementById('toast-data');
        const ok = holder?.dataset.ok;
        const err = holder?.dataset.err;
        if (ok) showToast(ok, 'success');
        if (err) showToast(err, 'error');
    });

    function showToast(message, type = 'success') {
        const t = document.createElement('div');
        t.className = `toast ${type}`;
        t.textContent = message;
        document.body.appendChild(t);
        setTimeout(() => t.classList.add('show'), 10);
        setTimeout(() => { t.classList.remove('show'); setTimeout(() => t.remove(), 500); }, 3800);
    }

})();
