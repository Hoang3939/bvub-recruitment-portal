// Văn bằng 

window.renderVBFields = function () {
    const container = document.getElementById("vanbang-container");
    container.innerHTML = "";

    const selected = Array.from(document.querySelectorAll(".vb-checkbox:checked"))
        .map(c => c.value);

    selected.forEach((loai, index) => {
        const isKhac = (loai === "Khác");
        const html = `
      <div class="vanbang-block">
        <h5>Văn bằng: ${loai}</h5>
        <input type="hidden" name="VanBangs[${index}].LoaiVanBang" value="${loai}" />
        ${isKhac ? `
          <div class="form-group">
            <label class="form-label">Tên văn bằng (bắt buộc khi chọn “Khác”)</label>
            <input class="form-control vb-khac" data-idx="${index}" type="text" placeholder="Ví dụ: Văn bằng 2, trung cấp"/>
          </div>
        ` : ""}

        <div class="form-group">
          <label class="form-label">Tên cơ sở cấp</label>
          <input name="VanBangs[${index}].TenCoSo" class="form-control" type="text" />
        </div>

        <div class="form-group">
          <label class="form-label">Ngày cấp</label>
          <input name="VanBangs[${index}].NgayCap" class="form-control js-date" type="text" autocomplete="off"/>
        </div>

        <div class="form-group">
          <label class="form-label">Số hiệu</label>
          <input name="VanBangs[${index}].SoHieu" class="form-control" type="text" />
        </div>

        <div class="form-group">
          <label class="form-label">Chuyên ngành đào tạo</label>
          <input name="VanBangs[${index}].ChuyenNganhDaoTao" class="form-control" type="text" />
        </div>

        <div class="form-group">
          <label class="form-label">Ngành đào tạo</label>
          <input name="VanBangs[${index}].NganhDaoTao" class="form-control" type="text" />
        </div>

        <div class="form-group">
          <label class="form-label">Hình thức đào tạo</label>
          <div class="choice-vertical">
            <label class="choice-item"><input type="radio" name="VanBangs[${index}].HinhThucDaoTao" value="Chính quy" class="radio-solid"> Chính quy</label>
            <label class="choice-item"><input type="radio" name="VanBangs[${index}].HinhThucDaoTao" value="Tại chức" class="radio-solid"> Tại chức</label>
            <label class="choice-item"><input type="radio" name="VanBangs[${index}].HinhThucDaoTao" value="Liên thông" class="radio-solid"> Liên thông</label>
            <label class="choice-item"><input type="radio" name="VanBangs[${index}].HinhThucDaoTao" value="Từ xa" class="radio-solid"> Từ xa</label>
            <label class="choice-item"><input type="radio" name="VanBangs[${index}].HinhThucDaoTao" value="Khác" class="radio-solid ht-khac" data-index="${index}"> Khác:</label>
          </div>
          <input type="text" class="form-control mt-2" name="VanBangs[${index}].HinhThucDaoTaoKhac" placeholder="Nhập hình thức đào tạo khác" style="display:none;" />
        </div>

        <div class="form-group">
          <label class="form-label">Xếp loại</label>
          <input name="VanBangs[${index}].XepLoai" class="form-control" type="text" />
        </div>
      </div>`;
        container.insertAdjacentHTML("beforeend", html);
        window.initDatepickers && window.initDatepickers(container);
    });
};

// Toggle “Khác” cho Hình thức đào tạo (văn bằng)
document.addEventListener("change", function (e) {
    if (e.target.matches('input[name^="VanBangs"][name$="].HinhThucDaoTao"]')) {
        const parent = e.target.closest(".form-group");
        const otherInput = parent.querySelector('input[name$=".HinhThucDaoTaoKhac"]');
        if (!otherInput) return;
        if (e.target.value === "Khác") {
            otherInput.style.display = "block";
        } else {
            otherInput.style.display = "none";
            otherInput.value = "";
        }
    }
});

// Bắt buộc ghi “Tên văn bằng” khi chọn Khác
(function attachOverrideOnSubmit() {
    const form = document.querySelector("form");
    if (!form) return;
    form.addEventListener("submit", function (e) {
        let ok = true;
        document.querySelectorAll(".vb-khac").forEach(input => {
            if (!input.value.trim()) {
                ok = false; input.focus();
                alert("Vui lòng nhập Tên văn bằng cho mục “Khác”.");
            } else {
                const idx = input.getAttribute("data-idx");
                const hidden = document.querySelector(`input[name="VanBangs[${idx}].LoaiVanBang"]`);
                if (hidden) hidden.value = input.value.trim();
            }
        });
        if (!ok) e.preventDefault();
    });
})();

// Trình độ văn hóa (hiện ô Khác) 
(function () {
    const otherBox = document.getElementById("txtTrinhDoKhac");
    function syncTDVH() {
        const picked = document.querySelector('input[name="DonVienChuc.TrinhDoVanHoa"]:checked');
        const isOther = picked && picked.value === "Khác";
        if (!otherBox) return;
        otherBox.style.display = isOther ? "block" : "none";
        if (!isOther) otherBox.value = "";
    }
    document.addEventListener("change", function (e) {
        if (e.target && e.target.name === "DonVienChuc.TrinhDoVanHoa") syncTDVH();
    });
    syncTDVH();
})();

// Tình trạng sức khỏe (khác)
(function () {
    const radios = document.querySelectorAll(".js-sk");
    const hid = document.getElementById("hidSK");
    const txt = document.getElementById("txtSkKhac");
    const rOther = document.getElementById("skKhac");
    const KNOWN = ["Loại I", "Loại II", "Loại III"];

    function reflectHiddenToUI() {
        const val = (hid?.value || "").trim();
        if (!txt || !hid) return;
        if (!val) {
            txt.style.display = "none"; txt.value = "";
            radios.forEach(r => r.checked = false);
            return;
        }
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

    window.setTinhTrangSucKhoe = function (value) {
        if (!hid) return;
        hid.value = (value || "").trim();
        reflectHiddenToUI();
    };

    reflectHiddenToUI();
})();

// Flow chức danh -> vị trí -> khoa phòng
$(function () {
    const $chucDanh = $("#ChucDanhId");
    const $viTri = $("#ViTriId");
    const $khoaPhong = $("#KhoaPhongId");

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

    // preload khi quay lại trang
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

// Khởi tạo mask cho các ô ngày ban đầu
window.initDatepickers && window.initDatepickers(document);