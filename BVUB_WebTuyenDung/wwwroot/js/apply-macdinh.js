window.Apply = window.Apply || {};

(function (ns) {
    function normalizeIso(iso) {
        if (!iso) return "";
        const m = String(iso).match(/^(\d{4}-\d{2}-\d{2})/); // lấy yyyy-MM-dd (cắt phần T.. nếu có)
        return m ? m[1] : "";
    }

    function setDateRobust(selector, iso) {
        const $el = $(selector);
        if (!$el.length) return;
        const el = $el[0];
        const val = normalizeIso(iso);

        // 1) flatpickr (phổ biến)
        if (el && el._flatpickr) {
            if (val) el._flatpickr.setDate(val, true);
            else el._flatpickr.clear();
            return;
        }

        // 2) bootstrap-datepicker (eternicode)
        if (typeof $el.datepicker === "function" && $el.data("datepicker")) {
            try {
                $el.datepicker("setDate", val ? new Date(val + "T00:00:00") : null);
                $el.datepicker("update");
            } catch (e) { /* bỏ qua */ }
            $el.trigger("change");
            return;
        }

        // 3) jQuery UI datepicker
        if (window.jQuery && $.ui && $.ui.datepicker &&
            typeof $el.datepicker === "function" && $el.hasClass("hasDatepicker")) {
            try {
                if (val) $el.datepicker("setDate", new Date(val + "T00:00:00"));
                else $el.val("");
            } catch (e) { /* bỏ qua */ }
            $el.trigger("change");
            return;
        }

        // 4) Không có datepicker: gán trực tiếp
        $el.val(val).trigger("input").trigger("change");
    }

    // Export helper (để debug nếu cần)
    ns.setDateRobust = setDateRobust;

    // phát hiện form mode
    ns.detectMode = function () {
        const fromAttr = ($("form").data("mode") || "").toString().toLowerCase();
        if (fromAttr === "vc" || fromAttr === "nld") return fromAttr;
        return $("#ChucDanhId").length ? "vc" : "nld";
    };

    ns.state = { lastCheckedEmail: null, canSubmit: false };

    // hiển thị hint cạnh ô email
    ns.setHint = function (selectorOr$Hint, msg, isErr) {
        const $h = selectorOr$Hint.jquery ? selectorOr$Hint : $(selectorOr$Hint);
        $h.text(msg || "")
            .toggleClass("text-danger", !!isErr)
            .toggleClass("text-muted", !isErr);
    };

    // Điền dữ liệu ứng viên
    ns.fillUngVien = function (u) {
        if (!u) return;

        // text/select cơ bản
        $('[name="UngVien.HoTen"]').val(u.hoTen ?? u.HoTen ?? "");
        $('[name="UngVien.GioiTinh"]').val(u.gioiTinh ?? u.GioiTinh ?? 0);
        $('[name="UngVien.SoDienThoai"]').val(u.soDienThoai ?? u.SoDienThoai ?? "");
        $('[name="UngVien.Email"]').val(u.email ?? u.Email ?? "");
        $('[name="UngVien.CCCD"]').val(u.cccd ?? u.CCCD ?? "");
        $('[name="UngVien.NoiCapCCCD"]').val(u.noiCapCCCD ?? u.NoiCapCCCD ?? "");
        $('[name="UngVien.DiaChiThuongTru"]').val(u.diaChiThuongTru ?? u.DiaChiThuongTru ?? "");
        $('[name="UngVien.DiaChiCuTru"]').val(u.diaChiCuTru ?? u.DiaChiCuTru ?? "");
        $('[name="UngVien.MaSoThue"]').val(u.maSoThue ?? u.MaSoThue ?? "");
        $('[name="UngVien.SoTaiKhoan"]').val(u.soTaiKhoan ?? u.SoTaiKhoan ?? "");
        $('[name="UngVien.TrinhDoChuyenMon"]').val(u.trinhDoChuyenMon ?? u.TrinhDoChuyenMon ?? "");

        // ngày: nhận cả u.ngaySinh (json thường) lẫn u.NgaySinh (phòng trường hợp key viết hoa)
        setDateRobust('[name="UngVien.NgaySinh"]', u.ngaySinh ?? u.NgaySinh);
        setDateRobust('[name="UngVien.NgayCapCCCD"]', u.ngayCapCCCD ?? u.NgayCapCCCD);

        // Tình trạng sức khỏe (nếu có UI radio)
        const sk = (u.tinhTrangSucKhoe ?? u.TinhTrangSucKhoe ?? "").trim();
        if (window.setTinhTrangSucKhoe) window.setTinhTrangSucKhoe(sk);
    };
})(window.Apply);