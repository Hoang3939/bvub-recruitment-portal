// DÙNG CHUNG CHO NLD & VC
(function () {
    const $email = $("#txtEmail");
    const $hint = $("#emailHint");
    const emailRegex = /^[^@\s]+@[^@\s]+\.[^@\s]+$/i;

    const mode = ($('form[data-mode]').data('mode') || '').toLowerCase(); // 'nld' | 'vc'
    let lastCheckedEmail = null;
    let canSubmit = false;

    function setHint(msg, isErr) {
        $hint.text(msg || "")
            .toggleClass("text-danger", !!isErr)
            .toggleClass("text-muted", !isErr);
    }

    // 💡 Không còn hàm fillUngVien nội bộ

    $email.on("input", function () {
        lastCheckedEmail = null;
        canSubmit = false;
        setHint("");
    });

    $("#btnCheckEmail").on("click", function () {
        const email = ($email.val() || "").trim();
        if (!email) { setHint("Vui lòng nhập Email trước.", true); return; }
        if (!emailRegex.test(email)) { setHint("Sai định dạng Email.", true); return; }

        setHint("Đang kiểm tra...");
        $.getJSON("/UngTuyen/CheckEmail", { email })
            .done(function (res) {
                lastCheckedEmail = email;

                if (!res || !res.ok) {
                    setHint(res?.message || "Không kiểm tra được Email.", true);
                    canSubmit = false;
                    return;
                }

                // Chưa có ai dùng
                if (!res.exists) {
                    setHint("Email hợp lệ và chưa tồn tại đơn. Bạn có thể nộp đơn.", false);
                    canSubmit = true;
                    return;
                }

                // ĐÃ có ứng viên -> dùng helper đã chuẩn hóa (điền cả ngày)
                if (res.ungVien && window.Apply && typeof Apply.fillUngVien === "function") {
                    Apply.fillUngVien(res.ungVien);
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
