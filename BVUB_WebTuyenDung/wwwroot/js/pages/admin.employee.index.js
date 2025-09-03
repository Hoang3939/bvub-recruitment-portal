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

    // <-- Popup xem chi tiết -->
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

    // <-- Xóa nhân viên -->
    (function () {
        document.addEventListener('click', async function (e) {
            const btn = e.target.closest('.btn-delete');
            if (!btn) return;

            const row = btn.closest('tr[data-id]');
            if (!row) return;
            const id = row.getAttribute('data-id');

            if (!confirm('Bạn có chắc muốn xóa nhân viên #' + id + ' ?')) return;

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
                    alert('Bạn không có quyền xóa.'); return;
                }

                const data = await resp.json().catch(() => null);
                if (!resp.ok || !data?.ok) {
                    alert((data && (data.message || '')) || 'Xóa thất bại'); return;
                }

                row.remove();
            } catch {
                alert('Có lỗi khi xóa.');
            }
        });
    })();

})();
