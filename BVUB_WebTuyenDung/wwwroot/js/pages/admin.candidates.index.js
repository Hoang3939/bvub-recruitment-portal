(function () {
    function cfg() {
        const el = document.getElementById('candidates-index-config'); if (!el) return {};
        const r = k => el.dataset[k] || '';
        return {
            detailsPartial: r('urlDetailsPartial'),
            approveNow: r('urlApprove'),
            unapproveNow: r('urlUnapprove'),
            cancelNow: r('urlCancel'),
            restoreNow: r('urlRestore'),
            vcDetailsTmpl: r('urlVcDetailsTmpl'),
            hdDetailsTmpl: r('urlHdDetailsTmpl')
        };
    }
    function getToken() {
        const f = document.querySelector('#af-index input[name="__RequestVerificationToken"]');
        return f ? f.value : '';
    }
    function buildAppUrl(type, id, C) {
        if (!type || !id) return null;
        if (type === 'VC' && C.vcDetailsTmpl) return C.vcDetailsTmpl.replace('__ID__', id);
        if (type === 'HD' && C.hdDetailsTmpl) return C.hdDetailsTmpl.replace('__ID__', id);
        return null;
    }
    function modalApi(rootId) {
        const root = document.getElementById(rootId), closeEls = root ? root.querySelectorAll('.modal-close') : [];
        function open() { if (root) { root.classList.add('show'); document.body.style.overflow = 'hidden'; } }
        function close() { if (root) { root.classList.remove('show'); document.body.style.overflow = ''; } }
        closeEls.forEach(el => el.addEventListener('click', close));
        root && root.addEventListener('click', e => { if (e.target === root) close(); });
        return { open, close, root };
    }
    function toastMini(msg) {
        const t = document.createElement('div');
        t.className = 'toast-mini'; t.textContent = msg || 'OK';
        Object.assign(t.style, { position: 'fixed', right: '16px', bottom: '16px', background: '#111', color: '#fff', padding: '8px 12px', borderRadius: '6px', opacity: '0', transition: 'opacity .2s' });
        document.body.appendChild(t); requestAnimationFrame(() => t.style.opacity = '1');
        setTimeout(() => { t.style.opacity = '0'; t.addEventListener('transitionend', () => t.remove(), { once: true }); }, 1500);
    }

    // ===== Modal chi tiết (click hàng để xem) =====
    (function initDetails() {
        const C = cfg();
        const M = modalApi('detailModal');
        const box = document.getElementById('detailContainer');
        const viewBtn = document.getElementById('viewApplicationBtn');
        const editBtn = document.getElementById('editBtnInModal');

        async function loadDetails(row) {
            const uvId = row.getAttribute('data-id');
            const donType = row.getAttribute('data-dontype');
            const donId = row.getAttribute('data-donid');
            const editUrl = row.getAttribute('data-editurl');

            if (box) box.innerHTML = 'Đang tải...';

            try {
                const url = C.detailsPartial + '?id=' + encodeURIComponent(uvId);
                const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                box.innerHTML = resp.ok ? await resp.text() : 'Không tải được dữ liệu.';
            } catch { box.innerHTML = 'Có lỗi khi tải dữ liệu.'; }

            const appUrl = buildAppUrl(donType, donId, C);
            if (appUrl) { viewBtn.style.display = ''; viewBtn.href = appUrl; } else { viewBtn.style.display = 'none'; }

            if (editUrl) { editBtn.style.display = ''; editBtn.href = editUrl; } else { editBtn.style.display = 'none'; }

            M.open();
        }

        document.addEventListener('click', function (e) {
            const interactive = e.target.closest('button, a, .btn-icon, svg, path, input, select');
            if (interactive) return;
            const row = e.target.closest('tr[data-id]'); if (!row) return;
            loadDetails(row);
        });
    })();

    // ===== Duyệt / Bỏ duyệt (hai popup tách biệt) =====
    (function initApproveFlows() {
        const C = cfg();

        const MApprove = modalApi('approveConfirm');
        const MUnapprove = modalApi('unapproveConfirm');
        const approveViewBtn = document.getElementById('approveViewBtn');

        const ctx = { row: null, donType: null, donId: null };

        // Mở popup DUYỆT
        document.addEventListener('click', function (e) {
            const btn = e.target.closest('.btn-approve'); if (!btn) return;
            const row = btn.closest('tr[data-id]'); if (!row) return;
            ctx.row = row;
            ctx.donType = row.getAttribute('data-dontype');
            ctx.donId = row.getAttribute('data-donid');

            // set link "Xem đơn"
            const url = buildAppUrl(ctx.donType, ctx.donId, C);
            if (url) { approveViewBtn.href = url; approveViewBtn.style.display = ''; } else { approveViewBtn.style.display = 'none'; }

            MApprove.open();
        });

        // DUYỆT NGAY
        document.getElementById('btnApproveNow')?.addEventListener('click', async function () {
            try {
                const resp = await fetch(C.approveNow, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': getToken()
                    },
                    body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                const data = await resp.json();
                if (!data.ok) return alert(data.message || 'Thao tác thất bại');

                const badge = ctx.row.querySelector('.status');
                if (badge) {
                    badge.textContent = data.newStatusLabel || 'Đã duyệt';
                    badge.classList.remove('pending', 'cancelled'); badge.classList.add('approved');
                }

                // đổi bộ nút: approved => Bỏ duyệt + Sửa + Hủy
                const cell = ctx.row.querySelector('td.cell-actions, td:last-child');
                if (cell) {
                    cell.innerHTML = `
                        <button class="btn btn-unapprove" type="button">Bỏ duyệt</button>
                        <a class="btn btn-primary" href="${ctx.row.getAttribute('data-editurl')}">Sửa</a>
                        <button class="btn btn-danger btn-cancel" type="button">Hủy</button>
                    `;
                }

                MApprove.close(); toastMini('Đã duyệt.');
            } catch { alert('Có lỗi khi cập nhật.'); }
        });

        // Mở popup BỎ DUYỆT
        document.addEventListener('click', function (e) {
            const btn = e.target.closest('.btn-unapprove'); if (!btn) return;
            const row = btn.closest('tr[data-id]'); if (!row) return;
            ctx.row = row;
            ctx.donType = row.getAttribute('data-dontype');
            ctx.donId = row.getAttribute('data-donid');
            MUnapprove.open();
        });

        // BỎ DUYỆT
        document.getElementById('btnUnapproveNow')?.addEventListener('click', async function () {
            try {
                const resp = await fetch(C.unapproveNow, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': getToken()
                    },
                    body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                const data = await resp.json();
                if (!data.ok) return alert(data.message || 'Thao tác thất bại');

                const badge = ctx.row.querySelector('.status');
                if (badge) {
                    badge.textContent = data.newStatusLabel || 'Chờ xử lý';
                    badge.classList.remove('approved', 'cancelled'); badge.classList.add('pending');
                }

                // đổi bộ nút: pending => Duyệt + Sửa + Hủy
                const cell = ctx.row.querySelector('td.cell-actions, td:last-child');
                if (cell) {
                    cell.innerHTML = `
                        <button class="btn btn-approve" type="button">Duyệt</button>
                        <a class="btn btn-primary" href="${ctx.row.getAttribute('data-editurl')}">Sửa</a>
                        <button class="btn btn-danger btn-cancel" type="button">Hủy</button>
                    `;
                }

                MUnapprove.close(); toastMini('Đã bỏ duyệt.');
            } catch { alert('Có lỗi khi cập nhật.'); }
        });
    })();

    // ===== Hủy / Khôi phục =====
    (function initCancelRestore() {
        const C = cfg();

        const Mc = modalApi('cancelConfirm');
        const Mr = modalApi('restoreConfirm');
        const ctx = { row: null, donType: null, donId: null };

        document.addEventListener('click', function (e) {
            const cancel = e.target.closest('.btn-cancel');
            const restore = e.target.closest('.btn-restore');
            const row = e.target.closest('tr[data-id]');
            if (!row) return;

            if (cancel) {
                ctx.row = row; ctx.donType = row.getAttribute('data-dontype'); ctx.donId = row.getAttribute('data-donid'); Mc.open();
            }
            if (restore) {
                ctx.row = row; ctx.donType = row.getAttribute('data-dontype'); ctx.donId = row.getAttribute('data-donid'); Mr.open();
            }
        });

        document.getElementById('btnCancelNow')?.addEventListener('click', async function () {
            try {
                const resp = await fetch(C.cancelNow, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': getToken()
                    },
                    body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                const data = await resp.json(); if (!data.ok) return alert(data.message || 'Hủy thất bại');

                const badge = ctx.row.querySelector('.status');
                if (badge) {
                    badge.textContent = data.newStatusLabel || 'Đã hủy';
                    badge.classList.remove('pending', 'approved'); badge.classList.add('cancelled');
                }
                // thay bộ nút: chỉ Khôi phục + Sửa
                const cell = ctx.row.querySelector('td.cell-actions, td:last-child');
                if (cell) {
                    cell.innerHTML = `
                        <button class="btn btn-restore" type="button">Khôi phục</button>
                        <a class="btn btn-primary" href="${ctx.row.getAttribute('data-editurl')}">Sửa</a>
                    `;
                }

                Mc.close(); toastMini('Đã hủy đơn.');
            } catch { alert('Có lỗi khi hủy.'); }
        });

        document.getElementById('btnRestoreNow')?.addEventListener('click', async function () {
            try {
                const resp = await fetch(C.restoreNow, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': getToken()
                    },
                    body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                const data = await resp.json(); if (!data.ok) return alert(data.message || 'Khôi phục thất bại');

                const badge = ctx.row.querySelector('.status');
                if (badge) {
                    badge.textContent = data.newStatusLabel || 'Đã duyệt';
                    badge.classList.remove('pending', 'cancelled'); badge.classList.add('approved');
                }
                // thay bộ nút: Bỏ duyệt + Sửa + Hủy (và ẩn nút khôi phục)
                const cell = ctx.row.querySelector('td.cell-actions, td:last-child');
                if (cell) {
                    cell.innerHTML = `
                        <button class="btn btn-unapprove" type="button">Bỏ duyệt</button>
                        <a class="btn btn-primary" href="${ctx.row.getAttribute('data-editurl')}">Sửa</a>
                        <button class="btn btn-danger btn-cancel" type="button">Hủy</button>
                    `;
                }

                Mr.close(); toastMini('Đã khôi phục.');
            } catch { alert('Có lỗi khi khôi phục.'); }
        });
    })();
})();
