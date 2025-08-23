// wwwroot/js/pages/admin.donvienchuc.edit.js
(function () {
    const cfg = document.getElementById('admin-vc-edit-config');
    if (!cfg) return;

    const todayStr = cfg.dataset.today || '';
    const backUrl = cfg.dataset.backUrl || '#';
    const urlViTri = cfg.dataset.urlVitriByChucdanh || '';
    const urlKhoaPg = cfg.dataset.urlKhoaphongByVitri || '';

    // ===== Helper popup
    function showPopup(title, msg) {
        let wrap = document.getElementById('saveResultModal');
        if (!wrap) {
            wrap = document.createElement('div');
            wrap.id = 'saveResultModal';
            wrap.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,.35);display:flex;align-items:center;justify-content:center;z-index:9999;';
            wrap.innerHTML = `
        <div style="background:#fff;border-radius:10px;box-shadow:0 10px 30px rgba(0,0,0,.15);max-width:520px;width:92%;padding:22px;">
          <h3 id="sr-title" style="margin:0 0 10px;font-size:22px;"></h3>
          <div id="sr-msg" style="white-space:pre-wrap;color:#333;margin-bottom:16px;"></div>
          <div style="display:flex;gap:10px;justify-content:flex-end">
            <a class="btn" id="sr-back">Quay lại</a>
            <button type="button" class="btn btn-primary" id="sr-close">Đóng</button>
          </div>
        </div>`;
            document.body.appendChild(wrap);
            wrap.addEventListener('click', e => { if (e.target === wrap) wrap.style.display = 'none'; });
            wrap.querySelector('#sr-close').addEventListener('click', () => wrap.style.display = 'none');
            wrap.querySelector('#sr-back').addEventListener('click', () => { window.location.assign(backUrl); });
        }
        wrap.querySelector('#sr-title').textContent = title || '';
        wrap.querySelector('#sr-msg').textContent = msg || '';
        wrap.style.display = 'flex';
    }

    // ===== chặn ngày tương lai
    (function () {
        const $dob = document.getElementById('NgaySinh');
        const $cccd = document.getElementById('NgayCapCCCD');
        [$dob, $cccd].forEach(el => {
            if (!el) return;
            if (todayStr) el.setAttribute('max', todayStr);
            el.addEventListener('change', function () {
                if (todayStr && this.value && this.value > todayStr) {
                    alert('Ngày không được lớn hơn hôm nay.');
                    this.value = todayStr;
                }
            });
        });
    })();

    // ===== Đồng bộ Sức khỏe
    (function () {
        const hid = document.getElementById('hidSK');
        const txt = document.getElementById('txtSkKhac');
        const radios = document.querySelectorAll('input[name="skUI"]');
        const KNOWN = ['Loại I', 'Loại II', 'Loại III'];

        function reflectHiddenToUI() {
            const val = (hid.value || '').trim();
            if (!val) { txt.style.display = 'none'; txt.value = ''; radios.forEach(r => r.checked = false); return; }
            const known = KNOWN.includes(val);
            radios.forEach(r => r.checked = (known ? r.value === val : r.value === '__OTHER__'));
            txt.style.display = known ? 'none' : 'block';
            if (!known) txt.value = val;
        }
        radios.forEach(r => r.addEventListener('change', function () {
            if (this.value === '__OTHER__') { txt.style.display = 'block'; hid.value = txt.value.trim(); }
            else { txt.style.display = 'none'; txt.value = ''; hid.value = this.value; }
        }));
        txt.addEventListener('input', function () {
            const other = document.getElementById('skKhac');
            if (other && other.checked) hid.value = this.value.trim();
        });
        if (hid) reflectHiddenToUI();
    })();

    // ===== Đồng bộ Trình độ văn hóa
    (function () {
        const hid = document.getElementById('hidTDVH');
        const txt = document.getElementById('txtTrinhDoKhac');
        const radios = document.querySelectorAll('input[name="tdvhUI"]');
        const KNOWN = ['12/12 Chính quy', '12/12 Bổ túc văn hóa'];

        function reflectHiddenToUI() {
            const val = (hid.value || '').trim();
            if (!val) { txt.style.display = 'none'; txt.value = ''; radios.forEach(r => r.checked = false); return; }
            const known = KNOWN.includes(val);
            radios.forEach(r => r.checked = (known ? r.value === val : r.value === '__OTHER__'));
            txt.style.display = known ? 'none' : 'block';
            if (!known) txt.value = val;
        }
        radios.forEach(r => r.addEventListener('change', function () {
            if (this.value === '__OTHER__') { txt.style.display = 'block'; hid.value = txt.value.trim(); }
            else { txt.style.display = 'none'; txt.value = ''; hid.value = this.value; }
        }));
        txt.addEventListener('input', function () {
            const other = document.getElementById('tdvhKhac');
            if (other && other.checked) hid.value = this.value.trim();
        });
        if (hid) reflectHiddenToUI();
    })();

    // ===== VB: Hình thức đào tạo (radio + ô khác)
    (function () {
        document.querySelectorAll('.vb-ht').forEach(r => {
            r.addEventListener('change', function () {
                const name = this.getAttribute('name'); // vb-ht-<i>
                const idx = (name || '').split('-').pop();
                const other = document.querySelector(`.vb-ht-other[data-index="${idx}"]`);
                const hid = document.querySelector(`.vb-ht-hidden[data-index="${idx}"]`);
                if (!hid || !other) return;
                if (this.value === '__OTHER__') {
                    other.style.display = 'block';
                    hid.value = (other.value || '').trim();
                } else {
                    other.style.display = 'none';
                    other.value = '';
                    hid.value = this.value;
                }
            });
        });
        document.querySelectorAll('.vb-ht-other').forEach(t => {
            t.addEventListener('input', function () {
                const idx = this.dataset.index;
                const hid = document.querySelector(`.vb-ht-hidden[data-index="${idx}"]`);
                const otherRadio = document.getElementById(`vbht-${idx}-5`);
                if (hid && otherRadio && otherRadio.checked) hid.value = this.value.trim();
            });
        });
    })();

    // ===== Cascade Chức danh -> Vị trí -> Khoa/Phòng
    (function () {
        const $cd = document.getElementById('ChucDanhId');
        const $vt = document.getElementById('ViTriId');
        const $kp = document.getElementById('KhoaPhongId');
        if (!$cd || !$vt || !$kp) return;

        function resetSelect(el, placeholder) {
            el.disabled = false;
            el.innerHTML = '';
            const opt = document.createElement('option'); opt.value = ''; opt.textContent = placeholder || '-- Chọn --';
            el.appendChild(opt);
        }
        function loading(el, text) {
            el.disabled = true;
            el.innerHTML = '';
            const opt = document.createElement('option'); opt.textContent = text || 'Đang tải...';
            el.appendChild(opt);
        }

        $cd.addEventListener('change', async function () {
            const idRaw = this.value;
            resetSelect($kp, '-- Chọn khoa/phòng --');
            loading($vt, 'Đang tải vị trí...');
            if (!idRaw) { resetSelect($vt, '-- Chọn vị trí --'); return; }

            try {
                const resp = await fetch(`${urlViTri}?chucDanhId=${encodeURIComponent(idRaw)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                const data = await resp.json();
                resetSelect($vt, '-- Chọn vị trí --');
                (data || []).forEach(x => {
                    const text = x.tenViTri ?? x.TenViTri;
                    const val = x.viTriId ?? x.ViTriId;
                    if (val != null) $vt.append(new Option(text, val));
                });
            } catch {
                resetSelect($vt, '-- Chọn vị trí --');
                alert('Không tải được danh sách vị trí');
            }
        });

        $vt.addEventListener('change', async function () {
            const idRaw = this.value;
            loading($kp, 'Đang tải khoa/phòng...');
            if (!idRaw) { resetSelect($kp, '-- Chọn khoa/phòng --'); return; }

            try {
                const resp = await fetch(`${urlKhoaPg}?viTriId=${encodeURIComponent(idRaw)}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                const data = await resp.json();
                resetSelect($kp, '-- Chọn khoa/phòng --');
                (data || []).forEach(x => {
                    const text = x.tenKhoaPhong ?? x.TenKhoaPhong;
                    const val = x.khoaPhongId ?? x.KhoaPhongId;
                    if (val != null) $kp.append(new Option(text, val));
                });
            } catch {
                resetSelect($kp, '-- Chọn khoa/phòng --');
                alert('Không tải được danh sách khoa/phòng');
            }
        });
    })();

    // ===== Submit AJAX
    (function () {
        const form = document.getElementById('frmCreateVC');
        if (!form) return;

        function getToken() { return form.querySelector('input[name="__RequestVerificationToken"]')?.value || ''; }

        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            // check ngày không vượt hôm nay
            const bad = [];
            const ns = document.getElementById('NgaySinh');
            const nc = document.getElementById('NgayCapCCCD');
            if (todayStr) {
                if (ns?.value && ns.value > todayStr) bad.push('Ngày sinh');
                if (nc?.value && nc.value > todayStr) bad.push('Ngày cấp CCCD');
            }
            if (bad.length) { alert(bad.join(', ') + ' không được lớn hơn hôm nay.'); return; }

            const fd = new FormData(form);
            const body = new URLSearchParams();
            for (const [k, v] of fd.entries()) body.append(k, v);

            let resp, data, text;
            try {
                resp = await fetch(form.action, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                        'RequestVerificationToken': getToken(),
                        'X-Requested-With': 'XMLHttpRequest',
                        'Accept': 'application/json'
                    },
                    body
                });
                const ct = resp.headers.get('content-type') || '';
                if (ct.includes('application/json')) data = await resp.json();
                else text = await resp.text();
            } catch (err) {
                showPopup('Lưu không thành công', 'Không gửi được yêu cầu.\n' + (err && err.message ? err.message : ''));
                return;
            }

            if (resp.ok && data?.ok) {
                showPopup('Đã lưu', data.message || 'Đã lưu thành công.');
            } else {
                const msg = (data && (data.message + (data.errors ? '\n' + data.errors : ''))) || text || ('Lỗi máy chủ: ' + resp.status);
                showPopup('Lưu không thành công', msg);
            }
        });
    })();
})();
