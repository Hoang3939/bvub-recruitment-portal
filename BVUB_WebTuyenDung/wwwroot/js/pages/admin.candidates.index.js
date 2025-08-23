// wwwroot/js/pages/admin.candidates.index.js
(function () {
    const $ = window.jQuery;

    function cfg() {
        const el = document.getElementById('candidates-index-config'); if (!el) return {};
        const r = k => el.dataset[k] || '';
        return {
            detailsPartial: r('urlDetailsPartial'), exportWord: r('urlExportWord'),
            approveNow: r('urlApprove'), cancelNow: r('urlCancel'),
            restoreNow: r('urlRestore'), deleteApp: r('urlDelete'),
            vcDetailsTmpl: r('urlVcDetailsTmpl'), hdDetailsTmpl: r('urlHdDetailsTmpl')
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
    function toastMini(msg) {
        const t = document.createElement('div'); t.className = 'toast-mini'; t.textContent = msg || 'Thao tác thành công.';
        document.body.appendChild(t); requestAnimationFrame(() => t.classList.add('show'));
        setTimeout(() => { t.classList.remove('show'); t.addEventListener('transitionend', () => t.remove(), { once: true }); }, 2200);
    }
    function modalApi(rootId) {
        const root = document.getElementById(rootId), closeEls = root ? root.querySelectorAll('.modal-close') : [];
        function open() { if (root) { root.classList.add('show'); document.body.style.overflow = 'hidden'; } }
        function close() { if (root) { root.classList.remove('show'); document.body.style.overflow = ''; } }
        closeEls.forEach(el => el.addEventListener('click', close));
        root && root.addEventListener('click', e => { if (e.target === root) close(); });
        return { open, close, root };
    }

    function initDetails(C) {
        const M = modalApi('detailModal'), box = document.getElementById('detailContainer');
        const exportBtn = document.getElementById('exportWordBtn'), viewBtn = document.getElementById('viewApplicationBtn');
        async function loadDetails(uvId, donType, donId, label) {
            if (box) { box.innerHTML = 'Đang tải...'; }
            try {
                const url = C.detailsPartial + '?id=' + encodeURIComponent(uvId);
                const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                box.innerHTML = resp.ok ? await resp.text() : 'Không tải được dữ liệu.';
            } catch { box.innerHTML = 'Có lỗi khi tải dữ liệu.'; }
            if (exportBtn) exportBtn.href = C.exportWord + '?id=' + encodeURIComponent(uvId);
            const appUrl = buildAppUrl(donType, donId, C);
            if (appUrl && viewBtn) { viewBtn.href = appUrl; viewBtn.textContent = label || (donType === 'VC' ? 'Đơn viên chức' : 'Hợp đồng lao động'); viewBtn.style.display = ''; }
            else if (viewBtn) { viewBtn.style.display = 'none'; viewBtn.removeAttribute('href'); }
            M.open();
        }
        document.addEventListener('click', e => {
            const interactive = e.target.closest('button, a, .btn-icon, svg, path'); if (interactive) return;
            const row = e.target.closest('tr[data-id]'); if (!row) return;
            loadDetails(row.getAttribute('data-id'), row.getAttribute('data-dontype'),
                row.getAttribute('data-donid'), row.getAttribute('data-loaidon'));
        });
    }

    function initApprove(C) {
        const M = modalApi('approveConfirm'), btnView = document.getElementById('btnViewBeforeApprove');
        const btnGo = document.getElementById('btnApproveNow'), ctx = { row: null, id: null, donType: null, donId: null };
        document.addEventListener('click', e => {
            const btn = e.target.closest('.btn-approve'); if (!btn) return;
            const row = btn.closest('tr[data-id]'); if (!row) return;
            ctx.row = row; ctx.id = row.getAttribute('data-id');
            ctx.donType = row.getAttribute('data-dontype'); ctx.donId = row.getAttribute('data-donid');
            M.open();
        });
        btnView && btnView.addEventListener('click', () => { const url = buildAppUrl(ctx.donType, ctx.donId, C); if (url) window.location.assign(url); });
        btnGo && btnGo.addEventListener('click', async () => {
            try {
                const resp = await fetch(C.approveNow, {
                    method: 'POST', headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'RequestVerificationToken': getToken()
                    }, body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                if (resp.status === 401 || resp.status === 403) return alert('Bạn không có quyền duyệt.');
                const data = await resp.json(); if (!data.ok) return alert(data.message || 'Duyệt thất bại');
                const badge = ctx.row.querySelector('.status');
                if (badge) { badge.textContent = data.newStatusLabel || 'Đã duyệt'; badge.classList.remove('pending', 'cancelled'); badge.classList.add(data.newStatusClass || 'approved'); }
                const cell = ctx.row.querySelector('td:last-child');
                if (cell) {
                    const approveBtn = cell.querySelector('.btn-approve');
                    if (approveBtn) { const a = document.createElement('a'); a.className = 'btn btn-primary'; a.href = ctx.row.getAttribute('data-editurl') || '#'; a.textContent = 'Sửa'; approveBtn.replaceWith(a); }
                }
                M.close(); toastMini('Đã duyệt thành công.');
            } catch { alert('Có lỗi khi duyệt.'); }
        });
    }

    function initCancel(C) {
        const M = modalApi('cancelConfirm'), btnClose = document.getElementById('btnCancelClose');
        const btnGo = document.getElementById('btnCancelNow'), ctx = { row: null, donType: null, donId: null };
        document.addEventListener('click', e => {
            const btn = e.target.closest('.btn-cancel'); if (!btn) return;
            const row = btn.closest('tr[data-id]'); if (!row) return;
            ctx.row = row; ctx.donType = row.getAttribute('data-dontype'); ctx.donId = row.getAttribute('data-donid');
            if (!ctx.donType || !ctx.donId) return alert('Thiếu thông tin đơn.'); M.open();
        });
        btnClose && btnClose.addEventListener('click', M.close);
        btnGo && btnGo.addEventListener('click', async () => {
            try {
                const resp = await fetch(C.cancelNow, {
                    method: 'POST', headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'RequestVerificationToken': getToken()
                    }, body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                if (resp.status === 401 || resp.status === 403) return alert('Bạn không có quyền hủy.');
                const data = await resp.json(); if (!data.ok) return alert(data.message || 'Hủy thất bại');
                const badge = ctx.row.querySelector('.status');
                if (badge) { badge.textContent = data.newStatusLabel || 'Đã hủy'; badge.classList.remove('pending', 'approved'); badge.classList.add(data.newStatusClass || 'cancelled'); }
                const cell = ctx.row.querySelector('td:last-child');
                cell && cell.querySelector('.btn-approve') && cell.querySelector('.btn-approve').remove();
                cell && cell.querySelector('.btn-cancel') && cell.querySelector('.btn-cancel').remove();
                cell && cell.querySelector('.btn.btn-primary') && cell.querySelector('.btn.btn-primary').remove();
                if (cell && !cell.querySelector('.btn-restore')) {
                    const r = document.createElement('button'); r.className = 'btn btn-restore'; r.type = 'button'; r.textContent = 'Khôi phục'; cell.insertBefore(r, cell.firstChild);
                }
                M.close();
            } catch { alert('Có lỗi khi hủy.'); }
        });
    }

    function initRestore(C) {
        const M = modalApi('restoreConfirm'), btnClose = document.getElementById('btnRestoreClose');
        const btnGo = document.getElementById('btnRestoreNow'), ctx = { row: null, donType: null, donId: null };
        document.addEventListener('click', e => {
            const btn = e.target.closest('.btn-restore'); if (!btn) return;
            const row = btn.closest('tr[data-id]'); if (!row) return;
            ctx.row = row; ctx.donType = row.getAttribute('data-dontype'); ctx.donId = row.getAttribute('data-donid');
            if (!ctx.donType || !ctx.donId) return alert('Thiếu thông tin đơn.'); M.open();
        });
        btnClose && btnClose.addEventListener('click', M.close);
        btnGo && btnGo.addEventListener('click', async () => {
            try {
                const resp = await fetch(C.restoreNow, {
                    method: 'POST', headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'RequestVerificationToken': getToken()
                    }, body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                if (resp.status === 401 || resp.status === 403) return alert('Bạn không có quyền khôi phục.');
                const data = await resp.json(); if (!data.ok) return alert(data.message || 'Khôi phục thất bại');
                const badge = ctx.row.querySelector('.status');
                if (badge) { badge.textContent = data.newStatusLabel || 'Đã duyệt'; badge.classList.remove('pending', 'cancelled'); badge.classList.add(data.newStatusClass || 'approved'); }
                const cell = ctx.row.querySelector('td:last-child');
                if (cell) {
                    const r = cell.querySelector('.btn-restore'); if (r) r.remove();
                    if (!cell.querySelector('.btn.btn-primary')) { const a = document.createElement('a'); a.className = 'btn btn-primary'; a.href = ctx.row.getAttribute('data-editurl') || '#'; a.textContent = 'Sửa'; cell.insertBefore(a, cell.firstChild); }
                    if (!cell.querySelector('.btn-cancel')) { const b = document.createElement('button'); b.type = 'button'; b.className = 'btn btn-danger btn-cancel'; b.textContent = 'Hủy'; cell.insertBefore(b, cell.querySelector('.btn.btn-primary')?.nextSibling || cell.firstChild); }
                }
                M.close(); toastMini('Khôi phục thành công.');
            } catch { alert('Có lỗi khi khôi phục.'); }
        });
    }

    function initDelete(C) {
        const M = modalApi('deleteConfirm'), btnClose = document.getElementById('btnDeleteClose');
        const btnGo = document.getElementById('btnDeleteNow'), ctx = { row: null, donType: null, donId: null };
        document.addEventListener('click', e => {
            const btn = e.target.closest('.btn-delete'); if (!btn) return;
            const row = btn.closest('tr[data-id]'); if (!row) return;
            ctx.row = row; ctx.donType = row.getAttribute('data-dontype'); ctx.donId = row.getAttribute('data-donid');
            if (!ctx.donType || !ctx.donId) return alert('Thiếu thông tin đơn.'); M.open();
        });
        btnClose && btnClose.addEventListener('click', M.close);
        btnGo && btnGo.addEventListener('click', async () => {
            try {
                const resp = await fetch(C.deleteApp, {
                    method: 'POST', headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'RequestVerificationToken': getToken()
                    }, body: new URLSearchParams({ donType: ctx.donType, id: ctx.donId })
                });
                if (resp.status === 401 || resp.status === 403) return alert('Bạn không có quyền xóa.');
                const data = await resp.json(); if (!data.ok) return alert(data.message || 'Xóa thất bại');
                ctx.row && ctx.row.remove(); M.close(); toastMini('Đã xóa đơn.');
            } catch { alert('Có lỗi khi xóa.'); }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        const C = cfg(); initDetails(C); initApprove(C); initCancel(C); initRestore(C); initDelete(C);
    });
})();
