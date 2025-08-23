// wwwroot/js/apply-date.js
(function () {
    function normalizeIso(iso) {
        if (!iso) return "";
        const m = String(iso).match(/^(\d{4}-\d{2}-\d{2})/);
        return m ? m[1] : "";
    }

    // Parse an toàn theo local: new Date(y, m-1, d)
    function safeDateFromIso(val) {
        const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(String(val || ""));
        if (!m) return null;
        return new Date(+m[1], +m[2] - 1, +m[3]); // tránh lệch năm/tháng
    }

    function setDateInput(selector, iso) {
        const $el = window.jQuery ? jQuery(selector) : null;
        const el = $el && $el.length ? $el[0] : document.querySelector(selector);
        if (!el) return;

        const val = normalizeIso(iso);
        const safe = val ? safeDateFromIso(val) : null;

        // 1) flatpickr
        if (el._flatpickr) {
            if (safe) el._flatpickr.setDate(safe, true);
            else el._flatpickr.clear();
            return;
        }

        // 2) bootstrap-datepicker (eternicode)
        if ($el && typeof $el.datepicker === "function" && $el.data("datepicker")) {
            if (safe) $el.datepicker("setDate", safe);
            else $el.datepicker("clearDates");
            $el.trigger("change");
            return;
        }

        // 3) jQuery UI datepicker
        if ($el && typeof $el.datepicker === "function" && $el.datepicker("widget")) {
            if (safe) $el.datepicker("setDate", safe);
            else $el.val("");
            $el.trigger("change");
            return;
        }

        // 4) plain input
        el.value = val || "";
        el.dispatchEvent(new Event("input", { bubbles: true }));
        el.dispatchEvent(new Event("change", { bubbles: true }));
    }

    // expose ra window để các file khác gọi
    window.setDateInput = setDateInput;
})();