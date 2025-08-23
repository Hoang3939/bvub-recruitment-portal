(function () {
    function wire(formId) {
        const f = document.getElementById(formId);
        if (!f) return;

        const $start = f.querySelector('[name="NgayDang"]');
        const $end = f.querySelector('[name="HanNopHoSo"]');

        function syncMin() {
            if ($start && $start.value) $end?.setAttribute('min', $start.value);
            else $end?.removeAttribute('min');
        }

        $start?.addEventListener('change', syncMin);
        syncMin();

        f.addEventListener('submit', function (e) {
            const d1 = new Date($start?.value || '');
            const d2 = new Date($end?.value || '');
            if (isFinite(d1) && isFinite(d2) && d2 < d1) {
                e.preventDefault();
                alert('Hạn nộp hồ sơ phải lớn hơn hoặc bằng Ngày đăng.');
            }
        });
    }

    wire('recFormCreate');
    wire('recFormEdit');
})();
