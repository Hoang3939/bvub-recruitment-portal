// Kiểm tra email + chặn submit (dùng chung 2 view)
(function (ns) {
    const $email = $("#txtEmail");
    const $hint = $("#emailHint");
    const emailRegex = /^[a-zA-Z0-9._%+-]+@gmail\.com$/i;
    const mode = ns.detectMode(); // "vc" hoặc "nld"

    $email.on("input", function () {
        ns.state.lastCheckedEmail = null;
        ns.state.canSubmit = false;
        ns.setHint($hint, "");
    });

    $("#btnCheckEmail").on("click", function () {
        const email = ($email.val() || "").trim();
        if (!email) { ns.setHint($hint, "Vui lòng nhập Email trước.", true); return; }
        if (!emailRegex.test(email)) { ns.setHint($hint, "Sai định dạng Email. Chỉ chấp nhận @gmail.com", true); return; }

        ns.setHint($hint, "Đang kiểm tra...");

        $.getJSON("/UngTuyen/CheckEmail", { email })
            .done(function (res) {
                ns.state.lastCheckedEmail = email;

                if (!res || !res.ok) {
                    ns.setHint($hint, res?.message || "Không kiểm tra được Email.", true);
                    ns.state.canSubmit = false;
                    return;
                }

                if (!res.exists) {
                    ns.setHint($hint, "Email chưa tồn tại trong hệ thống. Bạn có thể nhập mới và nộp đơn.", false);
                    ns.state.canSubmit = true;
                    return;
                }

                // đã tồn tại
                if (res.ungVien) ns.fillUngVien(res.ungVien);

                if (mode === "vc") {
                    if (res.hasDonVienChuc) {
                        ns.setHint($hint, "Email này đã có ĐƠN VIÊN CHỨC. Bạn không thể nộp thêm.", true);
                        ns.state.canSubmit = false;
                    } else {
                        ns.setHint($hint, "Đã tìm thấy hồ sơ ứng viên. Bạn có thể nộp đơn viên chức.", false);
                        ns.state.canSubmit = true;
                    }
                } else { // nld
                    if (res.hasHopDong) {
                        ns.setHint($hint, "Email này đã có hồ sơ NGƯỜI LAO ĐỘNG. Không thể nộp thêm.", true);
                        ns.state.canSubmit = false;
                    } else {
                        ns.setHint($hint, "Đã tìm thấy hồ sơ. Bạn có thể nộp đơn.", false);
                        ns.state.canSubmit = true;
                    }
                }
            })
            .fail(function () {
                ns.setHint($hint, "Lỗi kết nối. Vui lòng thử lại.", true);
                ns.state.canSubmit = false;
                ns.state.lastCheckedEmail = email;
            });
    });

    // Chặn submit nếu chưa kiểm tra / không đủ điều kiện
    $("form").on("submit", function (e) {
        const email = ($email.val() || "").trim();
        if (!ns.state.lastCheckedEmail || email !== ns.state.lastCheckedEmail) {
            e.preventDefault();
            ns.setHint($hint, 'Vui lòng nhập Email và bấm "Kiểm tra" trước khi nộp.', true);
            alert("Vui lòng kiểm tra Email trước khi nộp đơn.");
            return;
        }
        if (!ns.state.canSubmit) {
            e.preventDefault();
            ns.setHint($hint, "Bạn không đủ điều kiện nộp đơn với Email này.", true);
            alert("Bạn không đủ điều kiện nộp đơn với Email này.");
        }
    });

})(window.Apply);