// wwwroot/js/pages/admin.employee.index.js
(function () {
    const cfg = document.getElementById('admin-emp-index-config') || {};
    const detailsUrl = (cfg.dataset && cfg.dataset.detailsUrl) || '';
    const deleteUrl = (cfg.dataset && cfg.dataset.deleteUrl) || '';
    const toastMsg = (cfg.dataset && cfg.dataset.toastMsg) || '';
    const toastType = (cfg.dataset && cfg.dataset.toastType) || 'success';

    function getToken() {
        const f = document.querySelector('#af-emp-index input[name="__RequestVerificationToken"]');
        return f ? f.value : '';
    }

    // ===== Toast (góc phải) =====
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
        t.classList.remove('success', 'error');
        t.classList.add(type || 'success');
        t.querySelector('#niceToastMsg').textContent = msg || 'Thao tác thành công.';
        t.classList.add('show');
        const auto = setTimeout(() => t.classList.remove('show'), 2200);
        t.querySelector('.close').onclick = function () { clearTimeout(auto); t.classList.remove('show'); };
    }

    if (toastMsg) window.addEventListener('DOMContentLoaded', () => niceToast(toastMsg, toastType));

    // ===== Reusable Confirm Modal (không dùng window.confirm) =====
    function ensureConfirmModal() {
        let m = document.getElementById('niceConfirm');
        if (!m) {
            m = document.createElement('div');
            m.id = 'niceConfirm';
            m.className = 'modal-overlay';
            m.innerHTML = `
                <div class="modal-card" role="dialog" aria-modal="true" aria-labelledby="nc-title">
                    <div class="modal-header">
                        <h3 id="nc-title" class="modal-title">Xác nhận</h3>
                        <button type="button" class="modal-close" aria-label="Đóng">&times;</button>
                    </div>
                    <div class="modal-body">
                        <p id="nc-message">Bạn có chắc không?</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-export" id="nc-cancel">Hủy</button>
                        <button type="button" class="btn btn-danger" id="nc-ok">Xóa</button>
                    </div>
                </div>`;
            document.body.appendChild(m);
        }
        return m;
    }
    function confirmNice(message, okText) {
        return new Promise(resolve => {
            const modal = ensureConfirmModal();
            modal.querySelector('#nc-message').textContent = message || 'Bạn có chắc không?';
            modal.querySelector('#nc-ok').textContent = okText || 'Đồng ý';

            function open() { modal.classList.add('show'); document.body.style.overflow = 'hidden'; }
            function close() { modal.classList.remove('show'); document.body.style.overflow = ''; cleanup(); }
            function cleanup() {
                okBtn.removeEventListener('click', onOk);
                cancelBtn.removeEventListener('click', onCancel);
                closeBtn.removeEventListener('click', onCancel);
                modal.removeEventListener('click', onBackdrop);
                document.removeEventListener('keydown', onEsc);
            }
            function onOk() { resolve(true); close(); }
            function onCancel() { resolve(false); close(); }
            function onBackdrop(e) { if (e.target === modal) onCancel(); }
            function onEsc(e) { if (e.key === 'Escape') onCancel(); }

            const okBtn = modal.querySelector('#nc-ok');
            const cancelBtn = modal.querySelector('#nc-cancel');
            const closeBtn = modal.querySelector('.modal-close');

            okBtn.addEventListener('click', onOk);
            cancelBtn.addEventListener('click', onCancel);
            closeBtn.addEventListener('click', onCancel);
            modal.addEventListener('click', onBackdrop);
            document.addEventListener('keydown', onEsc);

            open();
        });
    }

    // ===== Popup xem chi tiết (giữ nguyên logic cũ) =====
    (function () {
        const modal = document.getElementById('empDetailModal');
        const container = document.getElementById('empDetailContainer');
        const editBtn = document.getElementById('empEditBtn');
        const closeEls = modal.querySelectorAll('.modal-close');

        function openModal() { modal.classList.add('show'); document.body.style.overflow = 'hidden'; }
        function closeModal() { modal.classList.remove('show'); document.body.style.overflow = ''; }
        closeEls.forEach(el => el.addEventListener('click', closeModal));
        modal.addEventListener('click', e => { if (e.target === modal) closeModal(); });

        function isInteractive(target) {
            return target.closest('a, button, .btn, .btn-icon, input, select, textarea, svg, path');
        }

        document.addEventListener('click', async function (e) {
            if (isInteractive(e.target)) return;
            const row = e.target.closest('tr[data-id]');
            if (!row) return;

            const id = row.getAttribute('data-id');
            const editUrl = row.getAttribute('data-editurl');
            if (!id) return;

            try {
                container.innerHTML = 'Đang tải...';
                editBtn.href = editUrl || '#';

                const url = detailsUrl + '?id=' + encodeURIComponent(id);
                const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                container.innerHTML = resp.ok ? await resp.text() : 'Không tải được dữ liệu.';
                openModal();
            } catch {
                container.innerHTML = 'Có lỗi khi tải dữ liệu.'; openModal();
            }
        });

        container.addEventListener('click', function (e) {
            const btn = e.target.closest('.toggle-pwd');
            if (!btn) return;

            const holder = btn.closest('.emp-item')?.querySelector('.pwd-mask');
            const icon = btn.querySelector('i');
            if (!holder || !icon) return;

            const shown = holder.getAttribute('data-shown') === '1';
            if (shown) {
                holder.textContent = '••••••••••••';
                holder.setAttribute('data-shown', '0');
                btn.setAttribute('aria-pressed', 'false');
                btn.setAttribute('aria-label', 'Hiện mật khẩu');
                icon.classList.remove('fa-eye'); icon.classList.add('fa-eye-slash');
            } else {
                const full = holder.getAttribute('data-full') || '';
                holder.textContent = full || '(trống)';
                holder.setAttribute('data-shown', '1');
                btn.setAttribute('aria-pressed', 'true');
                btn.setAttribute('aria-label', 'Ẩn mật khẩu');
                icon.classList.remove('fa-eye-slash'); icon.classList.add('fa-eye');
            }
        });
    })();

    // ===== Xóa nhân viên (modal + toast) =====
    (function () {
        document.addEventListener('click', async function (e) {
            const btn = e.target.closest('.btn-delete');
            if (!btn) return;

            const row = btn.closest('tr[data-id]');
            if (!row) return;
            const id = row.getAttribute('data-id');

            const ok = await confirmNice(`Bạn có chắc muốn xóa nhân viên #${id}?`, 'Xóa');
            if (!ok) return;

            try {
                const resp = await fetch(deleteUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': getToken()
                    },
                    body: new URLSearchParams({ id })
                });

                if (resp.status === 401 || resp.status === 403) {
                    niceToast('Bạn không có quyền xóa.', 'error'); return;
                }

                const data = await resp.json().catch(() => null);
                if (!resp.ok || !data?.ok) {
                    niceToast((data && (data.message || 'Xóa thất bại')) || 'Xóa thất bại', 'error');
                    return;
                }

                // Hiệu ứng mượt: fade-out rồi remove
                row.style.transition = 'opacity .2s ease, height .2s ease';
                row.style.opacity = '0';
                setTimeout(() => { row.remove(); }, 200);

                niceToast('Đã xóa nhân viên thành công.', 'success');
            } catch {
                niceToast('Có lỗi khi xóa.', 'error');
            }
        });
    })();

})();