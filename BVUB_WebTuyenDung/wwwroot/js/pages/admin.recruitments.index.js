document.addEventListener('DOMContentLoaded', function () {
    // <-- Config -->
    const cfg = document.getElementById('rec-index-config');
    const isAdmin = (cfg?.dataset.isAdmin === 'true');
    // Nếu detailsUrl rỗng (hiếm khi), fallback chuẩn cho Area
    const detailsUrlBase = cfg?.dataset.detailsUrl || '/Admin/Recruitments/DetailsPartial';
    const editTpl = cfg?.dataset.editUrlTemplate || '';
    const deleteUrl = cfg?.dataset.deleteUrl || '';

    // <-- Modal -->
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

    function getAntiForgeryToken() {
        const el = document.querySelector('#af-rec input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function looksLikeLoginPage(urlStr, html) {
        try {
            const url = new URL(urlStr, window.location.origin);
            const path = (url.pathname || '').toLowerCase();
            if (path.includes('/account') && path.includes('login')) return true;
        } catch { }
        // Phòng khi login view không ở /Account/Login
        return (html && html.toLowerCase().includes('name="password"')) || (html && html.toLowerCase().includes('đăng nhập'));
    }

    async function fetchDetailsHtml(id) {
        const token = getAntiForgeryToken();

        // ==== 1) POST + Token tới action chuyên AJAX (nếu tồn tại), nếu không có thì dùng chính DetailsPartial ====
        const postUrlCandidates = [
            detailsUrlBase.replace(/DetailsPartial\b/, 'DetailsPartialPost'),
            detailsUrlBase
        ];

        for (const postUrl of postUrlCandidates) {
            try {
                const resp = await fetch(postUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest',
                        'Accept': 'text/html'
                    },
                    body: new URLSearchParams({ id }),
                    credentials: 'same-origin'
                });

                const text = await resp.text().catch(() => '');

                // Bị chuyển hướng về trang login
                if (resp.status === 401 || resp.status === 403 || looksLikeLoginPage(resp.url, text)) {
                    return 'Phiên đăng nhập đã hết hạn hoặc thiếu quyền. Vui lòng tải lại trang và đăng nhập.';
                }

                // *** SỬA TẠI ĐÂY: chỉ cần có HTML là hiển thị, không phụ thuộc resp.ok ***
                if (text) return text;
                // Nếu không có nội dung → thử cách tiếp theo
            } catch (_) {
                // Thử cách tiếp theo
            }
        }

        // ==== 2) Fallback GET (không token, nhưng vẫn gửi cookie) ====
        try {
            const resp = await fetch(detailsUrlBase + '?id=' + encodeURIComponent(id), {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'text/html'
                },
                credentials: 'same-origin'
            });
            const text = await resp.text().catch(() => '');

            if (resp.status === 401 || resp.status === 403 || looksLikeLoginPage(resp.url, text)) {
                return 'Phiên đăng nhập đã hết hạn hoặc bạn không có quyền. Vui lòng tải lại trang và đăng nhập.';
            }

            // *** SỬA TẠI ĐÂY: chỉ cần có HTML là hiển thị, không phụ thuộc resp.ok ***
            if (text) return text;

            return 'Không tải được dữ liệu.';
        } catch {
            return 'Có lỗi khi tải dữ liệu.';
        }
    }

    // <-- Click card -> mở modal & nạp chi tiết -->
    document.addEventListener('click', async function (e) {
        const card = e.target.closest('.rec-card');
        if (!card || isInteractive(e.target)) return;

        currentId = card.getAttribute('data-id');
        if (!currentId) return;

        bodyEl.innerHTML = 'Đang tải...';
        openModal();

        const html = await fetchDetailsHtml(currentId);
        bodyEl.innerHTML = html || 'Không có dữ liệu để hiển thị.';

        if (isAdmin && editBtn) editBtn.href = editTpl.replace('__ID__', currentId);
    });

    // <-- Xóa -->
    delBtn?.addEventListener('click', async function () {
        if (!isAdmin || !currentId) return;
        if (!confirm('Xóa thông tin tuyển dụng này? Hành động không thể hoàn tác.')) return;

        const token = getAntiForgeryToken();

        try {
            const resp = await fetch(deleteUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': token
                },
                body: new URLSearchParams({ id: currentId }),
                credentials: 'same-origin'
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
});
