(function () {
    if (window.initDatepickers) {
        window.initDatepickers(document);
    }
})();

// Giữ tương thích onchange="renderVBFields()"
(function (ns) {
    window.renderVBFields = function () {
        if (ns && typeof ns.renderVBFields === "function") ns.renderVBFields();
    };
})(window.Apply);
