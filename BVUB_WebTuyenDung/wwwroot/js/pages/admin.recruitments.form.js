// wwwroot/js/pages/admin.recruitments.form.js
(function () {
    function wireCreateForm() {
        const form = document.getElementById('recFormCreate');
        if (!form) return;

        const modal = document.getElementById('recCreateConfirm'); // modal confirm
        if (!modal) return;

        const $start = form.querySelector('[name="NgayDang"]');
        const $end = form.querySelector('[name="HanNopHoSo"]');

        // --- Đồng bộ min cho Hạn nộp theo Ngày đăng ---
        function syncMin() {
            if ($start && $start.value) {
                $end && $end.setAttribute('min', $start.value);
            } else {
                $end && $end.removeAttribute('min');
            }
        }
        $start && $start.addEventListener('change', syncMin);
        syncMin();

        // --- Hàm parse an toàn yyyy-MM-dd ---
        function parseISODate(v) {
            if (!v) return NaN;
            // input type="date" trả yyyy-MM-dd → Date(v) hợp lệ
            const d = new Date(v);
            return isNaN(d.getTime()) ? NaN : d.getTime();
        }

        // --- Modal helpers ---
        const okBtn = modal.querySelector('#btnRecOk');
        const cancelBtn = modal.querySelector('#btnRecCancel');
        const closeBtn = modal.querySelector('.modal-close');

        function openModal() {
            modal.classList.add('show');
            document.body.style.overflow = 'hidden';
        }
        function closeModal() {
            modal.classList.remove('show');
            document.body.style.overflow = '';
        }

        [cancelBtn, closeBtn].forEach(b => b && b.addEventListener('click', closeModal));
        modal.addEventListener('click', (e) => { if (e.target === modal) closeModal(); });

        // --- 1 handler submit duy nhất ---
        form.addEventListener('submit', function (e) {
            // Nếu đã set cờ bỏ qua confirm -> cho submit thật
            if (form.dataset.skipConfirm === '1') {
                form.dataset.skipConfirm = '0';
                return; // không preventDefault
            }

            // Nếu form chưa hợp lệ HTML5 -> để trình duyệt hiển thị lỗi
            if (!form.checkValidity()) return;

            // Kiểm tra ngày (nếu cả 2 có giá trị)
            const t1 = parseISODate($start ? $start.value : '');
            const t2 = parseISODate($end ? $end.value : '');

            if (!isNaN(t1) && !isNaN(t2) && t2 < t1) {
                e.preventDefault();
                alert('Hạn nộp hồ sơ phải lớn hơn hoặc bằng Ngày đăng.');
                return;
            }

            // Mọi thứ OK -> chặn submit lần 1 để mở modal xác nhận
            e.preventDefault();
            openModal();
        });

        // Người dùng xác nhận Lưu trên modal
        okBtn && okBtn.addEventListener('click', function () {
            closeModal();
            // đặt cờ để bỏ qua confirm ở lần submit kế tiếp
            form.dataset.skipConfirm = '1';
            // Dùng requestSubmit để trigger validation native nếu còn
            if (typeof form.requestSubmit === 'function') form.requestSubmit();
            else form.submit();
        });
    }

    // Gắn cho trang Tạo (Create)
    wireCreateForm();

    // Nếu bạn có trang Edit, có thể thêm hàm wireEditForm() tương tự ở đây
})();
