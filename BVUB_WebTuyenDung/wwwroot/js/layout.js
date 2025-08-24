// wwwroot/js/layout.js
(function () {
    function getViLocale() {
        return (window.flatpickr && flatpickr.l10ns && (flatpickr.l10ns.vn || flatpickr.l10ns.vi)) || {
            firstDayOfWeek: 1,
            rangeSeparator: " đến ",
            weekAbbreviation: "Tu",
            scrollTitle: "Cuộn để thay đổi",
            toggleTitle: "Nhấn để đổi",
            time_24hr: true,
            weekdays: {
                shorthand: ["CN", "T2", "T3", "T4", "T5", "T6", "T7"],
                longhand: ["Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"]
            },
            months: {
                shorthand: ["Th 1", "Th 2", "Th 3", "Th 4", "Th 5", "Th 6", "Th 7", "Th 8", "Th 9", "Th 10", "Th 11", "Th 12"],
                longhand: ["Tháng một", "Tháng hai", "Tháng ba", "Tháng tư", "Tháng năm", "Tháng sáu",
                    "Tháng bảy", "Tháng tám", "Tháng chín", "Tháng mười", "Tháng mười một", "Tháng mười hai"]
            }
        };
    }

    window.initDatepickers = function (scope) {
        if (!window.flatpickr) return;
        const root = scope || document;
        const vi = getViLocale();

        // Đảm bảo global là tiếng Việt (phòng trường hợp lib nào đó gọi trước)
        if (flatpickr.localize) flatpickr.localize(vi);

        root.querySelectorAll('.js-date').forEach(el => {
            // Nếu đã có instance với locale default → hủy để tạo lại đúng cấu hình
            if (el._flatpickr) {
                try { el._flatpickr.destroy(); } catch { }
            }

            flatpickr(el, {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d/m/Y",
                altInputClass: "form-control",     // <— làm ô giống các ô khác
                allowInput: true,
                disableMobile: true,

                monthSelectorType: "dropdown",
                static: true,
                locale: vi,

                onReady: (sel, str, inst) => {
                    // gắn class để tăng chiều rộng header tránh che phần năm
                    inst.calendarContainer.classList.add("fp-wide");
                }
            });
        });
    };

    document.addEventListener('DOMContentLoaded', () => {
        window.initDatepickers(document);
    });
})();