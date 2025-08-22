// Chức danh -> Vị trí -> Khoa/Phòng (chỉ chạy nếu thấy đúng phần tử)
$(function () {
    const $chucDanh = $("#ChucDanhId");
    const $viTri = $("#ViTriId");
    const $khoaPhong = $("#KhoaPhongId");

    if (!$chucDanh.length || !$viTri.length || !$khoaPhong.length) {
        return; // view Người lao động không có 3 ô này
    }

    function reset($s, ph) {
        $s.prop("disabled", false).html('<option value="">' + (ph || "-- Chọn --") + "</option>");
    }
    function loading($s, t) {
        $s.prop("disabled", true).html("<option>" + (t || "Đang tải...") + "</option>");
    }

    $chucDanh.on("change", function () {
        const idRaw = $(this).val();
        const id = parseInt(idRaw, 10);
        loading($viTri, "Đang tải vị trí...");
        reset($khoaPhong, "-- Chọn khoa/phòng --");

        if (!idRaw || Number.isNaN(id)) { reset($viTri, "-- Chọn vị trí --"); return; }

        $.getJSON("/UngTuyen/GetViTriByChucDanh", { chucDanhId: id })
            .done(function (data) {
                reset($viTri, "-- Chọn vị trí --");
                if (!data || !data.length) { $viTri.append('<option value="">(Không có vị trí)</option>'); return; }
                data.forEach(function (x) {
                    const text = x.tenViTri ?? x.TenViTri;
                    const val = x.viTriId ?? x.ViTriId;
                    if (val != null) { $viTri.append(new Option(text, val)); }
                });
            })
            .fail(function () {
                reset($viTri, "-- Chọn vị trí --");
                alert("Không tải được danh sách vị trí");
            });
    });

    $viTri.on("change", function () {
        const idRaw = $(this).val();
        const id = parseInt(idRaw, 10);
        loading($khoaPhong, "Đang tải khoa/phòng...");

        if (!idRaw || Number.isNaN(id)) { reset($khoaPhong, "-- Chọn khoa/phòng --"); return; }

        $.getJSON("/UngTuyen/GetKhoaPhongByViTri", { viTriId: id })
            .done(function (data) {
                reset($khoaPhong, "-- Chọn khoa/phòng --");
                if (!data || !data.length) { $khoaPhong.append('<option value="">(Không có khoa/phòng)</option>'); return; }
                data.forEach(function (x) {
                    const text = x.tenKhoaPhong ?? x.TenKhoaPhong;
                    const val = x.khoaPhongId ?? x.KhoaPhongId;
                    if (val != null) { $khoaPhong.append(new Option(text, val)); }
                });
            })
            .fail(function () {
                reset($khoaPhong, "-- Chọn khoa/phòng --");
                alert("Không tải được danh sách khoa/phòng");
            });
    });

    // preload khi back trang / edit
    function preload() {
        const cdVal = $chucDanh.val();
        const vtVal = $viTri.val();
        const kpVal = $khoaPhong.val();

        if (!cdVal) return;

        $chucDanh.trigger("change");

        if (vtVal) {
            const t = setInterval(function () {
                if ($viTri.find("option").length > 1) {
                    clearInterval(t);
                    $viTri.val(vtVal).trigger("change");

                    if (kpVal) {
                        const t2 = setInterval(function () {
                            if ($khoaPhong.find("option").length > 1) {
                                clearInterval(t2);
                                $khoaPhong.val(kpVal);
                            }
                        }, 120);
                    }
                }
            }, 120);
        }
    }
    preload();
    window.addEventListener("pageshow", preload);
});