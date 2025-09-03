// wwwroot/js/apply-vanbang.js
(function (ns) {
    const qsa = (sel, ctx = document) => Array.from(ctx.querySelectorAll(sel));

    // bỏ dấu, gộp khoảng trắng, lower-case
    function keyify(s) {
        return (s || "")
            .toString()
            .normalize("NFD").replace(/[\u0300-\u036f]/g, "")
            .replace(/\s+/g, " ")
            .trim()
            .toLowerCase();
    }
    // CK I/II/III -> 1/2/3
    function normalizeCK(s) {
        let k = keyify(s);
        k = k.replace(/chuyen khoa i\b/g, "chuyen khoa 1")
            .replace(/chuyen khoa ii\b/g, "chuyen khoa 2")
            .replace(/chuyen khoa iii\b/g, "chuyen khoa 3");
        return k;
    }
    // Rút "loại" từ 1 item (string/object)
    function extractLoai(vb) {
        if (vb == null) return "";
        if (typeof vb === "string") return vb;
        return vb.LoaiVanBang ?? vb.loaiVanBang ?? vb.Loai ?? vb.TenLoai ?? vb.TenVanBang ?? "";
    }

    // Map tất cả checkbox -> nhiều key (value + label text)
    function getOptionMap() {
        const map = new Map();
        qsa(".vb-checkbox").forEach(cb => {
            const labelEl = cb.closest(".checkbox-item") || cb.parentElement;
            const labelText = (labelEl?.innerText || cb.value || "").replace(/^\s*[*•\-]?\s*/, "").trim();
            const candidates = new Set([
                cb.value,
                labelText,
                labelText.replace(/:$/, "")
            ]);
            candidates.forEach(txt => {
                const k = normalizeCK(txt);
                if (k) map.set(k, cb);
            });
        });
        return map;
    }

    // ===== 1) Render block input cho CÁC checkbox MỚI (không disabled) =====
    ns.renderVBFields = function () {
        const container = document.getElementById("vanbang-container");
        if (!container) return;
        container.innerHTML = "";

        const selected = qsa('.vb-checkbox:checked:not(:disabled)').map(c => c.value);

        selected.forEach((loai, index) => {
            const isKhac = keyify(loai) === keyify("Khác");
            const html = `
        <div class="vanbang-block">
          <h5>Văn bằng: ${loai}</h5>
          <input type="hidden" name="VanBangs[${index}].LoaiVanBang" value="${loai}" />
          ${isKhac ? `
          <div class="form-group">
            <label class="form-label">Tên văn bằng*</label>
            <input class="form-control vb-khac" data-idx="${index}" type="text"
                   placeholder="Ví dụ: Văn bằng 2, Trung cấp, ..." />
          </div>` : ""}

          <div class="form-group">
            <label class="form-label">Tên cơ sở cấp*</label>
            <input name="VanBangs[${index}].TenCoSo" class="form-control" type="text" />
          </div>

          <div class="form-group">
            <label class="form-label">Ngày cấp*</label>
            <input name="VanBangs[${index}].NgayCap" class="form-control js-date" type="text" autocomplete="off" />
          </div>

          <div class="form-group">
            <label class="form-label">Số hiệu*</label>
            <input name="VanBangs[${index}].SoHieu" class="form-control" type="text" />
          </div>

          <div class="form-group">
            <label class="form-label">Chuyên ngành đào tạo*</label>
            <input name="VanBangs[${index}].ChuyenNganhDaoTao" class="form-control" type="text" />
          </div>

          <div class="form-group">
            <label class="form-label">Ngành đào tạo*</label>
            <input name="VanBangs[${index}].NganhDaoTao" class="form-control" type="text" />
          </div>

          <div class="form-group">
            <label class="form-label">Hình thức đào tạo*</label>
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
            <label class="form-label">Xếp loại*</label>
            <input name="VanBangs[${index}].XepLoai" class="form-control" type="text" />
          </div>
        </div>`;
            container.insertAdjacentHTML("beforeend", html);
        });

        if (window.initDatepickers) window.initDatepickers(container);
    };

    // ===== 2) Tự tick + khóa (xám) những VB đã có =====
    ns.syncExistingReadonly = function (vanBangs) {
        const optionMap = getOptionMap();
        const notMatched = [];

        (vanBangs || []).forEach(vb => {
            const raw = extractLoai(vb);
            const k = normalizeCK(raw);
            const cb = optionMap.get(k);
            if (cb) {
                cb.checked = true;
                cb.disabled = true;
                const label = cb.closest(".checkbox-item") || cb.parentElement;
                if (label) label.classList.add("is-locked");
            } else {
                notMatched.push(raw);
            }
        });

        if (notMatched.length) {
            console.warn("Không map được các văn bằng:", notMatched);
        }

        // Chỉ render block cho checkbox đang mở (không disabled)
        ns.renderVBFields();
    };

    // ===== 3) Reset về trạng thái người mới =====
    ns.clearReadonly = function () {
        qsa(".vb-checkbox").forEach(cb => {
            cb.disabled = false;
            cb.checked = false;
            const label = cb.closest(".checkbox-item") || cb.parentElement;
            if (label) label.classList.remove("is-locked");
        });
        const container = document.getElementById("vanbang-container");
        if (container) container.innerHTML = "";
    };

    // ===== 4) UI handlers =====
    document.addEventListener("change", function (e) {
        if (e.target.matches(".vb-checkbox")) ns.renderVBFields();

        if (e.target.matches('input[name^="VanBangs"][name$=".HinhThucDaoTao"]')) {
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

    // ===== 5) Submit: nếu “Khác”, bắt buộc nhập tên & ghi đè LoaiVanBang =====
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

})(window.ApplyVB || (window.ApplyVB = {}));
