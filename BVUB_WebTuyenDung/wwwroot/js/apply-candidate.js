// wwwroot/js/apply-candidate.js
(function (ns) {
    // === util: format dd/MM/yyyy từ mọi kiểu chuỗi ngày ===
    function toDMY(val) {
        if (!val) return "";
        // nếu đã dd/MM/yyyy thì giữ nguyên
        if (typeof val === "string" && /\d{1,2}\/\d{1,2}\/\d{4}/.test(val)) return val;

        const d = new Date(val);
        if (isNaN(d)) return "";
        const dd = String(d.getDate()).padStart(2, "0");
        const mm = String(d.getMonth() + 1).padStart(2, "0");
        const yyyy = d.getFullYear();
        return `${dd}/${mm}/${yyyy}`;
    }

    // === 1) Điền dữ liệu ứng viên lên form ===
    ns.fillUngVien = function (uv) {
        if (!uv) return;

        const map = {
            'UngVien.HoTen': uv.HoTen,
            'UngVien.GioiTinh': uv.GioiTinh, // 0/1
            'UngVien.NgaySinh': toDMY(uv.NgaySinh),
            'UngVien.SoDienThoai': uv.SoDienThoai,
            'UngVien.Email': uv.Email,
            'UngVien.CCCD': uv.CCCD,
            'UngVien.NgayCapCCCD': toDMY(uv.NgayCapCCCD),
            'UngVien.NoiCapCCCD': uv.NoiCapCCCD,
            'UngVien.DiaChiThuongTru': uv.DiaChiThuongTru,
            'UngVien.DiaChiCuTru': uv.DiaChiCuTru,
            'UngVien.MaSoThue': uv.MaSoThue,
            'UngVien.SoTaiKhoan': uv.SoTaiKhoan,
            'UngVien.TrinhDoChuyenMon': uv.TrinhDoChuyenMon
        };

        Object.keys(map).forEach(name => {
            const el = document.querySelector(`[name="${name}"]`);
            if (!el) return;
            if (el.tagName === "SELECT") {
                el.value = map[name] ?? "";
            } else {
                el.value = map[name] ?? "";
            }
        });

        // Tình trạng sức khỏe
        const sk = (uv.TinhTrangSucKhoe || "").trim();
        const radios = document.querySelectorAll('.js-sk');
        let matched = false;
        radios.forEach(r => {
            if (r.value === sk) { r.checked = true; matched = true; }
            else if (r.id === "skKhac") r.checked = false;
        });

        const txtKhac = document.getElementById('txtSkKhac');
        const hid = document.getElementById('hidSK');
        if (matched) {
            if (txtKhac) { txtKhac.style.display = "none"; txtKhac.value = ""; }
            if (hid) hid.value = sk;
        } else {
            // set "Khác"
            const rdKhac = document.getElementById('skKhac');
            if (rdKhac) rdKhac.checked = true;
            if (txtKhac) {
                txtKhac.style.display = "block";
                txtKhac.value = sk; // hiển thị nội dung khác
            }
            if (hid) hid.value = sk;
        }

        // Khởi tạo lại datepicker nếu bạn dùng flatpickr
        if (window.initDatepickers) window.initDatepickers(document);
    };

    // === 2) Khóa/Mở các trường ứng viên ===
    ns.setCandidateReadOnly = function (on) {
        const sels = [
            'UngVien.HoTen', 'UngVien.GioiTinh', 'UngVien.NgaySinh', 'UngVien.SoDienThoai',
            'UngVien.CCCD', 'UngVien.NgayCapCCCD', 'UngVien.NoiCapCCCD',
            'UngVien.DiaChiThuongTru', 'UngVien.DiaChiCuTru',
            'UngVien.MaSoThue', 'UngVien.SoTaiKhoan', 'UngVien.TrinhDoChuyenMon'
        ];

        sels.forEach(name => {
            const el = document.querySelector(`[name="${name}"]`);
            if (!el) return;
            if (el.tagName === "SELECT") el.disabled = on;
            else el.readOnly = on;
            el.classList.toggle('is-readonly', on);
        });

        // Radio sức khỏe & ô khác
        document.querySelectorAll('.js-sk').forEach(r => r.disabled = on);
        const txtKhac = document.getElementById('txtSkKhac');
        if (txtKhac) {
            if (on) txtKhac.setAttribute('readonly', 'readonly');
            else txtKhac.removeAttribute('readonly');
        }

        // (tuỳ chọn) khoá luôn dropdown Loại hồ sơ
        const loai = document.getElementById('loaiHoSo');
        if (loai) loai.disabled = on;
    };

})(window.Apply || (window.Apply = {}));
