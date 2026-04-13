// wwwroot/js/apply-email.js
// DÙNG CHUNG CHO NLD & VC
(function () {
    const $email = $("#txtEmail");
    const $hint = $("#emailHint");
    const emailRegex = /^[^@\s]+@[^@\s]+\.[^@\s]+$/i;

    const mode = ($('form[data-mode]').data('mode') || '').toLowerCase(); // 'nld' | 'vc'
    let lastCheckedEmail = null;
    let canSubmit = false;
    let debounceTimer = null;
    let isChecking = false;

    // ---------------- helpers ----------------
    function setHint(msg, isErr) {
        $hint.text(msg || "")
            .toggleClass("text-danger", !!isErr)
            .toggleClass("text-muted", !isErr);
    }

    // Khóa/mở 2 ô ngày (không cho gõ & không mở lịch)
    function lockDates(lock) {
        ['UngVien.NgaySinh', 'UngVien.NgayCapCCCD'].forEach(name => {
            const el = document.querySelector(`input[name="${name}"]`);
            if (!el) return;
            el.readOnly = !!lock;
            if (el._flatpickr) el._flatpickr.set('clickOpens', !lock);
        });
    }

    // Khóa/mở nhóm radio "Tình trạng sức khỏe" mà vẫn giữ màu checked
    function lockHealth(lock) {
        document.querySelectorAll('input.js-sk[name="skUI"]').forEach(r => {
            if (lock) {
                r.dataset.locked = '1';
                r.tabIndex = -1;
            } else {
                delete r.dataset.locked;
                r.tabIndex = 0;
            }
        });
        const other = document.getElementById('txtSkKhac');
        if (other) other.readOnly = !!lock;
    }
    // Chặn tương tác khi radio đã khóa
    document.addEventListener('click', e => {
        const t = e.target.closest('input.js-sk[name="skUI"][data-locked="1"]');
        if (t) { e.preventDefault(); e.stopImmediatePropagation(); }
    }, true);
    document.addEventListener('keydown', e => {
        if (e.key === ' ' || e.keyCode === 32) {
            const t = e.target.closest('input.js-sk[name="skUI"][data-locked="1"]');
            if (t) { e.preventDefault(); e.stopImmediatePropagation(); }
        }
    }, true);

    // Khóa/mở thông tin Ứng viên (fallback an toàn nếu không có Apply.setCandidateReadOnly)
    function setCandidateReadOnly(lock) {
        if (window.Apply && typeof Apply.setCandidateReadOnly === "function") {
            Apply.setCandidateReadOnly(lock);
        } else {
            // Fallback: input -> readonly; SELECT -> không disable, chỉ "khóa mềm"
            document.querySelectorAll('[name^="UngVien."]').forEach(el => {
                if (el.id === 'txtEmail') return;
                if (el.tagName === 'SELECT') {
                    if (lock) {
                        el.dataset.locksel = '1';
                        el.dataset.selValue = el.value;
                        el.tabIndex = -1;
                    } else {
                        delete el.dataset.locksel;
                        delete el.dataset.selValue;
                        el.tabIndex = 0;
                    }
                } else {
                    el.readOnly = !!lock;
                }
            });
        }
        lockDates(lock);
        lockHealth(lock);
    }

    // Chặn mở & đổi lựa chọn cho select đã khóa (không dùng disabled để vẫn POST)
    document.addEventListener('mousedown', e => {
        const sel = e.target.closest('select[data-locksel="1"]');
        if (sel) { e.preventDefault(); sel.blur(); }
    }, true);
    document.addEventListener('keydown', e => {
        const sel = e.target.closest('select[data-locksel="1"]');
        if (sel) { e.preventDefault(); }
    }, true);
    document.addEventListener('change', e => {
        const sel = e.target.closest('select[data-locksel="1"]');
        if (sel && sel.dataset.selValue != null) sel.value = sel.dataset.selValue;
    }, true);

    // --------------- core check ----------------
    function doCheckEmail() {
        const email = ($email.val() || "").trim();

        // Không làm gì nếu rỗng hoặc sai format
        if (!email) { setHint(""); canSubmit = false; return; }
        if (!emailRegex.test(email)) { setHint("Sai định dạng Email.", true); canSubmit = false; return; }

        // Không gọi lại nếu email đã kiểm tra rồi
        if (email === lastCheckedEmail) return;

        isChecking = true;
        setHint("Đang kiểm tra...");
        $.getJSON("/UngTuyen/CheckEmail", { email })
            .done(function (res) {
                lastCheckedEmail = email;
                isChecking = false;

                if (!res || !res.ok) {
                    setHint(res?.message || "Không kiểm tra được Email.", true);
                    canSubmit = false;
                    return;
                }

                // ----- Email CHƯA tồn tại -----
                if (!res.exists) {
                    setHint("Email hợp lệ và chưa tồn tại đơn. Bạn có thể nộp đơn.", false);
                    canSubmit = true;
                    setCandidateReadOnly(false);
                    if (window.ApplyVB && typeof ApplyVB.clearReadonly === "function") {
                        ApplyVB.clearReadonly();
                    }
                    return;
                }

                // ----- Email ĐÃ có ứng viên -----
                if (res.ungVien && window.Apply && typeof Apply.fillUngVien === "function") {
                    Apply.fillUngVien(res.ungVien);
                }
                setCandidateReadOnly(true);

                if (Array.isArray(res.vanBangs) && window.ApplyVB && typeof ApplyVB.syncExistingReadonly === "function") {
                    ApplyVB.syncExistingReadonly(res.vanBangs); // tick & khóa checkbox VB đã có
                }

                // Quy tắc submit theo mode
                if (mode === "nld") {
                    if (res.hasHopDong) {
                        setHint("Email này đã có hồ sơ NGƯỜI LAO ĐỘNG. Không thể nộp thêm.", true);
                        canSubmit = false;
                    } else {
                        setHint("Đã tìm thấy hồ sơ. Thông tin Ứng viên đã được điền sẵn, bạn có thể nộp đơn.", false);
                        canSubmit = true;
                    }
                } else { // vc
                    if (res.hasDonVienChuc) {
                        setHint("Email này đã có ĐƠN VIÊN CHỨC. Bạn không thể nộp thêm.", true);
                        canSubmit = false;
                    } else {
                        setHint("Đã tìm thấy hồ sơ. Thông tin Ứng viên đã được điền sẵn, bạn có thể nộp đơn.", false);
                        canSubmit = true;
                    }
                }
            })
            .fail(function () {
                isChecking = false;
                setHint("Lỗi kết nối. Vui lòng thử lại.", true);
                canSubmit = false;
            });
    }

    // --------------- events ------------------
    // Đổi email -> reset & tự động kiểm tra sau 800ms (debounce)
    $email.on("input", function () {
        lastCheckedEmail = null;
        canSubmit = false;
        setHint("");

        setCandidateReadOnly(false);
        if (window.ApplyVB && typeof ApplyVB.clearReadonly === "function") {
            ApplyVB.clearReadonly();               // mở lại checkbox Văn bằng
        }

        // Debounce: tự động kiểm tra sau khi ngừng gõ 800ms
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(function () {
            doCheckEmail();
        }, 800);
    });

    // Kiểm tra ngay khi blur (rời khỏi ô email)
    $email.on("blur", function () {
        if (debounceTimer) clearTimeout(debounceTimer);
        doCheckEmail();
    });

    // Giữ tương thích: nút Kiểm tra vẫn hoạt động nếu còn tồn tại trên giao diện
    $("#btnCheckEmail").on("click", function () {
        if (debounceTimer) clearTimeout(debounceTimer);
        doCheckEmail();
    });

    // Chặn submit nếu chưa check email hoặc đang kiểm tra
    $("form").on("submit", function (e) {
        const email = ($email.val() || "").trim();
        if (isChecking) {
            e.preventDefault();
            setHint("Đang kiểm tra Email, vui lòng chờ...", true);
            return;
        }
        if (!lastCheckedEmail || email !== lastCheckedEmail) {
            e.preventDefault();
            setHint("Vui lòng nhập Email hợp lệ để hệ thống tự kiểm tra trước khi nộp.", true);
            $email.focus();
            return;
        }
        if (!canSubmit) {
            e.preventDefault();
            setHint("Bạn không đủ điều kiện nộp đơn với Email này.", true);
            return;
        }

        // Safety: nếu có chỗ nào vẫn disable select => thêm hidden mirror để POST
        this.querySelectorAll('select[disabled]').forEach(sel => {
            if (!sel.name) return;
            const mirror = document.createElement('input');
            mirror.type = 'hidden';
            mirror.name = sel.name;
            mirror.value = sel.value || '';
            this.appendChild(mirror);
        });
    });
})();