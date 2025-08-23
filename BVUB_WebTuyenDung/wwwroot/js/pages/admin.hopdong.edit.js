// wwwroot/js/pages/admin.hopdong.edit.js

// Đồng bộ Tình trạng sức khỏe (hidden <-> radio + input khác)
(function () {
    const hid = document.getElementById('hidSK');
    const txt = document.getElementById('txtSkKhac');
    const radios = document.querySelectorAll('input[name="skUI"]');
    const rOther = document.getElementById('skKhac');
    const KNOWN = ['Loại I', 'Loại II', 'Loại III'];

    function reflect() {
        const v = (hid?.value || '').trim();
        if (!hid || !txt) return;
        if (!v) {
            txt.style.display = 'none'; txt.value = '';
            radios.forEach(r => r.checked = false);
            return;
        }
        if (KNOWN.includes(v)) {
            txt.style.display = 'none'; txt.value = '';
            radios.forEach(r => r.checked = (r.value === v));
        } else {
            if (rOther) rOther.checked = true;
            txt.style.display = 'block'; txt.value = v;
        }
    }

    radios.forEach(r => r.addEventListener('change', function () {
        if (!hid || !txt) return;
        if (this.value === '__OTHER__') {
            txt.style.display = 'block';
            hid.value = (txt.value || '').trim();
        } else {
            txt.style.display = 'none'; txt.value = '';
            hid.value = this.value;
        }
    }));
    txt?.addEventListener('input', function () {
        if (!hid) return;
        if (rOther?.checked) hid.value = this.value.trim();
    });

    reflect();
})();

// Submit AJAX + dialog kết quả
(function () {
    const form = document.getElementById('frmHDLD');
    if (!form) return;

    const dlg = document.getElementById('saveDialog');
    const dlgTitle = document.getElementById('dlgTitle');
    const dlgMsg = document.getElementById('dlgMsg');
    const btnClose = document.getElementById('dlgClose');

    btnClose?.addEventListener('click', () => dlg?.close());

    function openDialog(title, html) {
        if (!dlg || !dlgTitle || !dlgMsg) return;
        dlgTitle.textContent = title || 'Kết quả';
        dlgMsg.innerHTML = html || '';
        if (!dlg.open) dlg.showModal(); else dlg.show();
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const fd = new FormData(form);

        try {
            const resp = await fetch(form.action, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json'
                },
                body: fd
            });

            const ct = resp.headers.get('content-type') || '';
            let ok = resp.ok;
            let title = ok ? 'Lưu thành công' : 'Lưu không thành công';
            let msg = ok ? 'Đã lưu thành công.' : ('Lỗi máy chủ: ' + resp.status);

            if (ct.includes('application/json')) {
                const data = await resp.json().catch(() => ({}));
                ok = !!data.ok;
                title = ok ? 'Lưu thành công' : 'Lưu không thành công';
                msg = data.message || (ok ? 'Đã lưu thành công.' : 'Lưu không thành công.');
                if (!ok && data.errors) msg += '<br/><small class="text-muted">' + data.errors + '</small>';
            }

            openDialog(title, msg);
        } catch {
            openDialog('Lưu không thành công', 'Có lỗi kết nối. Vui lòng thử lại.');
        }
    });
})();
