// CHECK EMAIL + FILL
(function () {
    const $email = $("#txtEmail");
    const $hint = $("#emailHint");
    const emailRegex = /^[a-zA-Z0-9._%+-]+@gmail\.com$/i;

    let lastCheckedEmail = null;
    let canSubmit = false;

    function setHint(msg, isErr) {
        $hint.text(msg || "")
            .toggleClass("text-danger", !!isErr)
            .toggleClass("text-muted", !isErr);
    }

    function fillUngVien(u) {
        if (!u) return;
        $('[name="UngVien.HoTen"]').val(u.hoTen ?? u.HoTen ?? "");
        $('[name="UngVien.GioiTinh"]').val(u.gioiTinh ?? u.GioiTinh ?? 0);
        window.setDateInput('[name="UngVien.NgaySinh"]', u.NgaySinh ?? u.ngaySinh);
        $('[name="UngVien.SoDienThoai"]').val(u.soDienThoai ?? u.SoDienThoai ?? "");
        $('[name="UngVien.Email"]').val(u.email ?? u.Email ?? "");
        $('[name="UngVien.CCCD"]').val(u.cccd ?? u.CCCD ?? "");
        window.setDateInput('[name="UngVien.NgayCapCCCD"]', u.NgayCapCCCD ?? u.ngayCapCCCD);
        $('[name="UngVien.NoiCapCCCD"]').val(u.noiCapCCCD ?? u.NoiCapCCCD ?? "");
        $('[name="UngVien.DiaChiThuongTru"]').val(u.diaChiThuongTru ?? u.DiaChiThuongTru ?? "");
        $('[name="UngVien.DiaChiCuTru"]').val(u.diaChiCuTru ?? u.DiaChiCuTru ?? "");
        $('[name="UngVien.MaSoThue"]').val(u.maSoThue ?? u.MaSoThue ?? "");
        $('[name="UngVien.SoTaiKhoan"]').val(u.soTaiKhoan ?? u.SoTaiKhoan ?? "");
        $('[name="UngVien.TrinhDoChuyenMon"]').val(u.trinhDoChuyenMon ?? u.TrinhDoChuyenMon ?? "");
        const sk = (u.tinhTrangSucKhoe ?? u.TinhTrangSucKhoe ?? "").trim();
        if (window.setTinhTrangSucKhoe) window.setTinhTrangSucKhoe(sk);
    }

    $email.on("input", function () {
        lastCheckedEmail = null;
        canSubmitForNLD = false;
        setHint("");
    });

    $("#btnCheckEmail").on("click", function () {
        const email = ($email.val() || "").trim();
        if (!email) { setHint("Vui lòng nhập Email trước.", true); return; }
        if (!emailRegex.test(email)) { setHint("Sai định dạng Email. Chỉ chấp nhận @gmail.com", true); return; }

        setHint("Đang kiểm tra...");
        $.getJSON("/UngTuyen/CheckEmail", { email })
            .done(function (res) {
                lastCheckedEmail = email;
                if (!res || !res.ok) {
                    setHint(res?.message || "Không kiểm tra được Email.", true);
                    canSubmit = false;
                    return;
                }

                if (!res.exists) {
                    setHint("Email hợp lệ và chưa tồn tại. Bạn có thể nộp đơn.", false);
                    canSubmit = true;
                    return;
                }

                if (res.ungVien) fillUngVien(res.ungVien);

                if (res.hasHopDong) {
                    setHint("Email này đã có hồ sơ NGƯỜI LAO ĐỘNG. Không thể nộp thêm.", true);
                    canSubmit = false;
                } else {
                    setHint("Đã tìm thấy hồ sơ. Thông tin Ứng viên đã được điền sẵn, bạn có thể nộp đơn.", false);
                    canSubmit = true;
                }
            })
            .fail(function () {
                setHint("Lỗi kết nối. Vui lòng thử lại.", true);
                canSubmit = false;
            });
    });

    $("form").on("submit", function (e) {
        const email = ($email.val() || "").trim();
        if (!lastCheckedEmail || email !== lastCheckedEmail) {
            e.preventDefault();
            setHint('Vui lòng nhập Email và bấm "Kiểm tra" trước khi nộp.', true);
            alert("Vui lòng kiểm tra Email trước khi nộp đơn.");
            return;
        }
        if (!canSubmit) {
            e.preventDefault();
            setHint("Bạn không đủ điều kiện nộp đơn với Email này.", true);
            alert("Bạn không đủ điều kiện nộp đơn với Email này.");
        }
    });
})();

// TÌNH TRẠNG SỨC KHỎE (ẩn/hiện “Khác”)
(function () {
    const radios = document.querySelectorAll(".js-sk");
    const hid = document.getElementById("hidSK");
    const txt = document.getElementById("txtSkKhac");
    const rOther = document.getElementById("skKhac");
    const KNOWN = ["Loại I", "Loại II", "Loại III"];

    function reflect() {
        const val = (hid?.value || "").trim();
        if (!hid || !txt) return;
        if (!val) { txt.style.display = "none"; txt.value = ""; radios.forEach(r => r.checked = false); return; }
        if (KNOWN.includes(val)) {
            txt.style.display = "none"; txt.value = "";
            radios.forEach(r => r.checked = (r.value === val));
        } else {
            if (rOther) rOther.checked = true;
            txt.style.display = "block";
            txt.value = val;
        }
    }

    radios.forEach(r => r.addEventListener("change", function () {
        if (!hid || !txt) return;
        if (this.value === "__OTHER__") {
            txt.style.display = "block";
            hid.value = txt.value.trim();
        } else {
            txt.style.display = "none";
            txt.value = "";
            hid.value = this.value;
        }
    }));

    if (txt && rOther) {
        txt.addEventListener("input", function () {
            if (rOther.checked && hid) hid.value = this.value.trim();
        });
    }

    window.setTinhTrangSucKhoe = function (sk) {
        if (!hid) return;
        hid.value = (sk || "").trim(); reflect();
    };
    reflect();
})();

// Khởi tạo mask cho các ô ngày ban đầu
window.initDatepickers && window.initDatepickers(document);