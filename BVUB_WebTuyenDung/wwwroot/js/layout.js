function initDatepickers(scope) {
    if (typeof flatpickr === "undefined") return;

    const vi = {
        weekdays: {
            shorthand: ["CN", "T2", "T3", "T4", "T5", "T6", "T7"],
            longhand: ["Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"]
        },
        months: {
            shorthand: ["Thg 1", "Thg 2", "Thg 3", "Thg 4", "Thg 5", "Thg 6", "Thg 7", "Thg 8", "Thg 9", "Thg 10", "Thg 11", "Thg 12"],
            longhand: ["Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7",
                "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"]
        },
        firstDayOfWeek: 1
    };
    flatpickr.localize(vi);

    const root = scope || document;

    root.querySelectorAll(".js-date").forEach(el => {
        if (el._flatpickr) return;

        flatpickr(el, {
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d/m/Y",
            allowInput: true,
            disableMobile: true,
            monthSelectorType: "static",
            appendTo: document.body
        });
    });
}

window.setDateInput = function (selector, isoOrText) {
    const el = document.querySelector(selector);
    if (!el) return;
    if (el._flatpickr) {
        el._flatpickr.setDate(isoOrText, true);
    } else {
        el.value = isoOrText || "";
    }
};

document.addEventListener("DOMContentLoaded", () => initDatepickers());
