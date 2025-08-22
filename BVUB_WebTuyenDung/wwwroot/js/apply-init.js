// Khởi tạo datepicker cho các ô .js-date khi mới load
(function () {
    if (window.initDatepickers) {
        window.initDatepickers(document);
    }
})();

// Expose để tương thích onchange="renderVBFields()" trong View
(function (ns) {
    window.renderVBFields = function () {
        if (ns && typeof ns.renderVBFields === "function") {
            ns.renderVBFields();
        }
    };
})(window.Apply);