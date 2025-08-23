// Render khối Văn bằng + toggle "Hình thức đào tạo: Khác" + validate
(function (ns) {
    ns.renderVBFields = function () {
        const container = document.getElementById("vanbang-container");
        if (!container) return;
        container.innerHTML = "";

        const selected = Array.from(document.querySelectorAll(".vb-checkbox:checked")).map(c => c.value);

        selected.forEach((loai, index) => {
            const isKhac = (loai === "Khác");
            const html = `
      <div class="vanbang-block">
        <h5>Văn bằng: ${loai}</h5>
        <input type="hidden" name="VanBangs[${index}].LoaiVanBang" value="${loai}" />

        ${isKhac ? `
          <div class="form-group">
            <label class="form-label">Tên văn bằng</label>
            <input class="form-control vb-khac" data-idx="${index}" type="text" placeholder="Ví dụ: Văn bằng 2, Trung cấp, ..."/>
          </div>` : ""}

        <div class="form-group">
          <label class="form-label">Tên cơ sở cấp</label>
          <input name="VanBangs[${index}].TenCoSo" class="form-control" type="text" />
        </div>

        <div class="form-group">
          <label class="form-label">Ngày cấp</label>
          <input name="VanBangs[${index}].NgayCap" class="form-control js-date" type="text" autocomplete="off" />
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
          <input type="text" class="form-control mt-2"
                 name="VanBangs[${index}].HinhThucDaoTaoKhac"
                 placeholder="Nhập hình thức đào tạo khác"
                 style="display:none;" />
        </div>

        <div class="form-group">
          <label class="form-label">Xếp loại</label>
          <input name="VanBangs[${index}].XepLoai" class="form-control" type="text" />
        </div>
      </div>`;
            container.insertAdjacentHTML("beforeend", html);
            if (window.initDatepickers) window.initDatepickers(container);
        });
    };

    // Toggle “Khác” cho Hình thức đào tạo (văn bằng)
    document.addEventListener("change", function (e) {
        if (e.target.matches('input[name^="VanBangs"][name$="].HinhThucDaoTao"]')) {
            const parent = e.target.closest(".form-group");
            const otherInput = parent?.querySelector('input[name$=".HinhThucDaoTaoKhac"]');
            if (!otherInput) return;
            if (e.target.value === "Khác") {
                otherInput.style.display = "block";
            } else {
                otherInput.style.display = "none";
                otherInput.value = "";
            }
        }
    });

    // Bắt buộc nhập “Tên văn bằng” khi chọn Khác
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
                    if (hidden) hidden.value = input.value.trim(); // ghi đè chữ "Khác"
                }
            });
            if (!ok) e.preventDefault();
        });
    })();

})(window.Apply || (window.Apply = {}));