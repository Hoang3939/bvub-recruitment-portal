(function () {
    function parseISO(dateStr) {
        // kỳ vọng "yyyy-MM-dd"
        if (!dateStr || typeof dateStr !== "string") return null;
        const m = dateStr.match(/^(\d{4})-(\d{2})-(\d{2})$/);
        if (!m) return null;
        const d = new Date(+m[1], +m[2] - 1, +m[3]);
        return Number.isNaN(d.getTime()) ? null : d;
    }

    // Giá trị hôm nay (00:00)
    const todayStr = new Date().toISOString().slice(0, 10);
    const today = parseISO(todayStr);

    // 1) Nếu bạn dùng jQuery UI datepicker:
    if (window.jQuery && jQuery.fn.datepicker) {
        // đặt maxDate = 0 cho mọi .js-date
        jQuery(function ($) {
            $(".js-date").each(function () {
                try { $(this).datepicker("option", "maxDate", 0); } catch (_) { }
            });
        });
    }

    // 2) Nếu bạn dùng bootstrap-datepicker:
    if (window.jQuery && jQuery.fn.datepicker && jQuery.fn.datepicker.Constructor && jQuery.fn.datepicker.Constructor.VERSION) {
        jQuery(function ($) {
            $(".js-date").each(function () {
                try { $(this).datepicker("setEndDate", new Date()); } catch (_) { }
            });
        });
    }

    // 3) Nếu bạn có hook initDatepickers(container), cố gắng gọi lại với cấu hình max:
    if (typeof window.initDatepickers === "function") {
        try { window.initDatepickers(document, { max: new Date() }); } catch (_) { }
    }

    // 4) Dù dùng datepicker gì, vẫn thêm lớp phòng thủ khi user gõ tay:
    document.addEventListener("change", function (e) {
        const el = e.target;
        if (!el.classList || !el.classList.contains("js-date")) return;
        const val = (el.value || "").trim();
        const d = parseISO(val);
        if (!d) return; // không phải định dạng ISO, tùy bạn đổi parse nếu cần

        if (d > today) {
            alert("Ngày không được lớn hơn ngày hiện tại.");
            // reset về hôm nay hoặc xóa:
            el.value = todayStr; // hoặc: el.value = "";
            el.dispatchEvent(new Event("input", { bubbles: true }));
        }
    });
})();