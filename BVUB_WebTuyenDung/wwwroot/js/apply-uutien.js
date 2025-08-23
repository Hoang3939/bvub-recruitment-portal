(function () {
    const $hid = $("#hidUuTien");
    const $other = $("#txtUuTienOther");
    const $rdOth = $("#rdUuTienOther");
    const $group = $("#uutien-group");

    if (!$group.length || !$hid.length) return;

    function syncFromUI() {
        const picked = $group.find('input[name="DoiTuongUuTienUI"]:checked').val();
        if (picked === "__OTHER__") {
            $other.show().trigger("focus");
            // nếu người dùng chưa gõ gì, hidden tạm rỗng để server bắt Required
            $hid.val(($other.val() || "").trim());
        } else {
            $other.hide().val("");
            $hid.val(picked || "");
        }
    }

    // thay đổi radio
    $group.on("change", 'input[name="DoiTuongUuTienUI"]', syncFromUI);

    // gõ vào ô “Khác”
    $other.on("input", function () {
        if ($rdOth.is(":checked")) {
            $hid.val($(this).val().trim());
        }
    });

    // nếu trang back/Refill từ ModelState, cố gắng phản chiếu ra UI
    (function preload() {
        const val = ($hid.val() || "").trim();
        if (!val) return;
        const $target = $group.find(`input[value="${val}"]`);
        if ($target.length) {
            $target.prop("checked", true);
            $other.hide().val("");
        } else {
            $rdOth.prop("checked", true);
            $other.show().val(val);
        }
    })();
})();