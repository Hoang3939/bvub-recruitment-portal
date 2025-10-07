// wwwroot/js/pages/admin.donvienchuc.edit.js
(function () {
    const cfg = document.getElementById('admin-vc-edit-config');
    if (!cfg) return;

    const todayStr = cfg.dataset.today || '';
    const backUrl = cfg.dataset.backUrl || '#';
    const urlViTri = cfg.dataset.urlVitriByChucdanh || '';
    const urlKhoaPg = cfg.dataset.urlKhoaphongByVitri || '';

    // ========== Utils ==========
    function toast(text) {
        // mini toast gọn nhẹ
        let t = document.getElementById('miniToast');
        if (!t) {
            t = document.createElement('div');
            t.id = 'miniToast';
            t.style.cssText = 'position:fixed;right:16px;bottom:16px;background:#111;color:#fff;padding:8px 12px;border-radius:8px;opacity:0;transition:opacity .15s;z-index:99999';
            document.body.appendChild(t);
        }
        t.textContent = text || 'OK';
        requestAnimationFrame(() => t.style.opacity = '1');
        setTimeout(() => { t.style.opacity = '0'; }, 1600);
    }

    function getToken(form) {
        return form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    // ========== Giới hạn ngày (không cho > hôm nay) ==========
    (function limitDates() {
        const ids = ['NgaySinh', 'NgayCapCCCD'];
        ids.forEach(id => {
            const el = document.getElementById(id);
            if (el && todayStr) el.setAttribute('max', todayStr);
        });
    })();

    // ========== Đồng bộ Sức khỏe ==========
    (function bindSK() {
        const hid = document.getElementById('hidSK');
        const txt = document.getElementById('txtSkKhac');
        const radios = document.querySelectorAll('input[name="skUI"]');
        if (!hid || !txt || !radios.length) return;
        const KNOWN = ['Loại I', 'Loại II', 'Loại III'];

        function reflect() {
            const val = (hid.value || '').trim();
            if (!val) { txt.style.display = 'none'; txt.value = ''; radios.forEach(r => r.checked = false); return; }
            const known = KNOWN.includes(val);
            radios.forEach(r => r.checked = known ? (r.value === val) : (r.value === '__OTHER__'));
            txt.style.display = known ? 'none' : 'block';
            if (!known) txt.value = val;
        }
        radios.forEach(r => r.addEventListener('change', function () {
            if (this.value === '__OTHER__') { txt.style.display = 'block'; hid.value = txt.value.trim(); }
            else { txt.style.display = 'none'; txt.value = ''; hid.value = this.value; }
        }));
        txt.addEventListener('input', function () {
            const rd = document.getElementById('skKhac');
            if (rd?.checked) hid.value = this.value.trim();
        });
        reflect();
    })();

    // ========== Đồng bộ Trình độ văn hóa ==========
    (function bindTDVH() {
        const hid = document.getElementById('hidTDVH');
        const txt = document.getElementById('txtTrinhDoKhac');
        const radios = document.querySelectorAll('input[name="tdvhUI"]');
        if (!hid || !txt || !radios.length) return;
        const KNOWN = ['12/12 Chính quy', '12/12 Bổ túc văn hóa'];

        function reflect() {
            const val = (hid.value || '').trim();
            if (!val) { txt.style.display = 'none'; txt.value = ''; radios.forEach(r => r.checked = false); return; }
            const known = KNOWN.includes(val);
            radios.forEach(r => r.checked = known ? (r.value === val) : (r.value === '__OTHER__'));
            txt.style.display = known ? 'none' : 'block';
            if (!known) txt.value = val;
        }
        radios.forEach(r => r.addEventListener('change', function () {
            if (this.value === '__OTHER__') { txt.style.display = 'block'; hid.value = txt.value.trim(); }
            else { txt.style.display = 'none'; txt.value = ''; hid.value = this.value; }
        }));
        txt.addEventListener('input', function () {
            const rd = document.getElementById('tdvhKhac');
            if (rd?.checked) hid.value = this.value.trim();
        });
        reflect();
    })();

    // ========== VB: hình thức đào tạo ==========
    (function bindVBHT() {
        document.querySelectorAll('.vb-ht').forEach(r => {
            r.addEventListener('change', function () {
                const name = this.getAttribute('name'); // vb-ht-<i>
                const idx = (name || '').split('-').pop();
                const other = document.querySelector(`.vb-ht-other[data-index="${idx}"]`);
                const hid = document.querySelector(`.vb-ht-hidden[data-index="${idx}"]`);
                if (!other || !hid) return;
                if (this.value === '__OTHER__') { other.style.display = 'block'; hid.value = (other.value || '').trim(); }
                else { other.style.display = 'none'; other.value = ''; hid.value = this.value; }
            });
        });
        document.querySelectorAll('.vb-ht-other').forEach(t => {
            t.addEventListener('input', function () {
                const idx = this.dataset.index;
                const hid = document.querySelector(`.vb-ht-hidden[data-index="${idx}"]`);
                const rd = document.getElementById(`vbht-${idx}-5`);
                if (hid && rd?.checked) hid.value = this.value.trim();
            });
        });
    })();

    // ========== Cascade Chức danh -> Vị trí -> Khoa/Phòng ==========
    (function cascades() {
        const $cd = document.getElementById('ChucDanhId');
        const $vt = document.getElementById('ViTriId');
        const $kp = document.getElementById('KhoaPhongId');
        if (!$cd || !$vt || !$kp) return;

        function resetSelect(el, placeholder) {
            el.disabled = false; el.innerHTML = '';
            el.append(new Option(placeholder || '-- Chọn --', ''));
        }
        function loading(el, text) {
            el.disabled = true; el.innerHTML = '';
            const o = new Option(text || 'Đang tải...', ''); el.append(o);
        }

        $cd.addEventListener('change', async function () {
            resetSelect($kp, '-- Chọn khoa/phòng --');
            const id = this.value;
            if (!id) { resetSelect($vt, '-- Chọn vị trí --'); return; }
            loading($vt, 'Đang tải vị trí...');
            try {
                const resp = await fetch(`${urlViTri}?chucDanhId=${encodeURIComponent(id)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                const data = await resp.json();
                resetSelect($vt, '-- Chọn vị trí --');
                (data || []).forEach(x => $vt.append(new Option(x.tenViTri ?? x.TenViTri, x.viTriId ?? x.ViTriId)));
            } catch { resetSelect($vt, '-- Chọn vị trí --'); toast('Không tải được vị trí'); }
        });

        $vt.addEventListener('change', async function () {
            const id = this.value;
            if (!id) { resetSelect($kp, '-- Chọn khoa/phòng --'); return; }
            loading($kp, 'Đang tải khoa/phòng...');
            try {
                const resp = await fetch(`${urlKhoaPg}?viTriId=${encodeURIComponent(id)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                const data = await resp.json();
                resetSelect($kp, '-- Chọn khoa/phòng --');
                (data || []).forEach(x => $kp.append(new Option(x.tenKhoaPhong ?? x.TenKhoaPhong, x.khoaPhongId ?? x.KhoaPhongId)));
            } catch { resetSelect($kp, '-- Chọn khoa/phòng --'); toast('Không tải được khoa/phòng'); }
        });
    })();

    // ========== CONFIRM + AJAX SUBMIT (1 luồng duy nhất) ==========
    (function saveFlow() {
        const form = document.getElementById('frmCreateVC');
        const modal = document.getElementById('saveConfirm');
        if (!form || !modal) return;

        const okBtn = modal.querySelector('#btnOkSave');
        const cancelBtn = modal.querySelector('#btnCancelSave');
        const xBtn = modal.querySelector('.modal-close');

        function open() { modal.classList.add('show'); document.body.style.overflow = 'hidden'; }
        function close() { modal.classList.remove('show'); document.body.style.overflow = ''; }

        [cancelBtn, xBtn].forEach(b => b && b.addEventListener('click', close));
        modal.addEventListener('click', e => { if (e.target === modal) close(); });

        // Chỉ mở confirm – KHÔNG submit thật
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            // cho browser hiện lỗi required nếu có
            if (!form.reportValidity()) return;
            open();
        });

        // Bấm "Lưu" trong modal -> submit AJAX
        okBtn && okBtn.addEventListener('click', async function () {
            close();

            // kiểm tra ngày > hôm nay
            const bad = [];
            const ns = document.getElementById('NgaySinh')?.value;
            const nc = document.getElementById('NgayCapCCCD')?.value;
            if (todayStr) {
                if (ns && ns > todayStr) bad.push('Ngày sinh');
                if (nc && nc > todayStr) bad.push('Ngày cấp CCCD');
            }
            if (bad.length) { toast(bad.join(', ') + ' không được lớn hơn hôm nay.'); return; }

            // gửi AJAX
            const fd = new FormData(form);
            const body = new URLSearchParams();
            for (const [k, v] of fd.entries()) body.append(k, v);

            try {
                const resp = await fetch(form.action, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': getToken(form),
                        'X-Requested-With': 'XMLHttpRequest',
                        'Accept': 'application/json'
                    },
                    body
                });
                const ct = resp.headers.get('content-type') || '';
                const data = ct.includes('application/json') ? await resp.json() : null;

                if (resp.ok && data?.ok) {
                    toast('Đã lưu.');
                    setTimeout(() => window.location.assign(backUrl), 800);
                } else {
                    const msg = (data && (data.message + (data.errors ? '\n' + data.errors : ''))) || ('Lỗi máy chủ: ' + resp.status);
                    alert('Lưu không thành công\n\n' + msg);
                }
            } catch (err) {
                alert('Lưu không thành công\n\n' + (err?.message || 'Không gửi được yêu cầu'));
            }
        });
    })();
})();