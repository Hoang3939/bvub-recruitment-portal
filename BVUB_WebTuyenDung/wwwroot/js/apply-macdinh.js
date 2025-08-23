// Namespace gọn gàng
window.Apply = window.Apply || {};

(function (ns) {
    // phát hiện form mode
    ns.detectMode = function () {
        const fromAttr = ($("form").data("mode") || "").toString().toLowerCase();
        if (fromAttr === "vc" || fromAttr === "nld") return fromAttr;
        return $("#ChucDanhId").length ? "vc" : "nld";
    };

    // state chung cho trang
    ns.state = {
        lastCheckedEmail: null,
        canSubmit: false
    };

    // hiển thị hint cạnh ô email
    ns.setHint = function (selectorOr$Hint, msg, isErr) {
        const $h = selectorOr$Hint.jquery ? selectorOr$Hint : $(selectorOr$Hint);
        $h.text(msg || "")
            .toggleClass("text-danger", !!isErr)
            .toggleClass("text-muted", !isErr);
    };

    // Điền dữ liệu ứng viên (giữ nguyên mapping)
    ns.fillUngVien = function (u) {
        if (!u) return;
        $('[name="UngVien.HoTen"]').val(u.hoTen ?? u.HoTen ?? "");
        $('[name="UngVien.GioiTinh"]').val(u.gioiTinh ?? u.GioiTinh ?? 0);

        if (window.setDateInput) window.setDateInput('[name="UngVien.NgaySinh"]', u.NgaySinh ?? u.ngaySinh);

        $('[name="UngVien.SoDienThoai"]').val(u.soDienThoai ?? u.SoDienThoai ?? "");
        $('[name="UngVien.Email"]').val(u.email ?? u.Email ?? "");
        $('[name="UngVien.CCCD"]').val(u.cccd ?? u.CCCD ?? "");

        if (window.setDateInput) window.setDateInput('[name="UngVien.NgayCapCCCD"]', u.NgayCapCCCD ?? u.ngayCapCCCD);

        $('[name="UngVien.NoiCapCCCD"]').val(u.noiCapCCCD ?? u.NoiCapCCCD ?? "");
        $('[name="UngVien.DiaChiThuongTru"]').val(u.diaChiThuongTru ?? u.DiaChiThuongTru ?? "");
        $('[name="UngVien.DiaChiCuTru"]').val(u.diaChiCuTru ?? u.DiaChiCuTru ?? "");
        $('[name="UngVien.MaSoThue"]').val(u.maSoThue ?? u.MaSoThue ?? "");
        $('[name="UngVien.SoTaiKhoan"]').val(u.soTaiKhoan ?? u.SoTaiKhoan ?? "");
        $('[name="UngVien.TrinhDoChuyenMon"]').val(u.trinhDoChuyenMon ?? u.TrinhDoChuyenMon ?? "");

        const sk = (u.tinhTrangSucKhoe ?? u.TinhTrangSucKhoe ?? "").trim();
        if (window.setTinhTrangSucKhoe) window.setTinhTrangSucKhoe(sk);
    };

})(window.Apply);