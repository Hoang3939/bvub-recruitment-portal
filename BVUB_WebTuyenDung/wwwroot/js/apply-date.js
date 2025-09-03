(function () {
    // Cắt chuẩn về "yyyy-MM-dd"
    function normalizeYmd(s) {
        if (!s) return "";
        const m = String(s).match(/^(\d{4}-\d{2}-\d{2})/);
        return m ? m[1] : "";
    }

    // API duy nhất: set giá trị ngày cho input
    window.setDateInput = function (selector, iso) {
        const el = document.querySelector(selector);
        if (!el) return;

        const ymd = normalizeYmd(iso);

        // Nếu input đã được flatpickr khởi tạo
        if (el._flatpickr) {
            if (ymd) el._flatpickr.setDate(ymd, true);
            else el._flatpickr.clear();
            return;
        }

        // Input thường (chưa có flatpickr)
        el.value = ymd || "";
        el.dispatchEvent(new Event("input", { bubbles: true }));
        el.dispatchEvent(new Event("change", { bubbles: true }));
    };
})();