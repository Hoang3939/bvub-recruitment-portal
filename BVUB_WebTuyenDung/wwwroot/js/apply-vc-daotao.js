/**
 * apply-vc-daotao.js
 * Logic checkbox nhiều giá trị cho "Có tham gia đào tạo" trong form Viên chức.
 * Khi checkbox thay đổi → join tất cả giá trị đã chọn bằng ", " → set vào hidden input.
 * Khi chọn "Khác" → hiện textbox nhập tự do, include giá trị vào string.
 */
(function () {
    'use strict';

    var hidField = document.getElementById('hidThamGiaDaoTao');
    var cbKhac = document.getElementById('cbDaoTaoKhac');
    var txtKhac = document.getElementById('txtDaoTaoKhac');
    var checkboxes = document.querySelectorAll('.cb-daotao');

    if (!hidField || !checkboxes.length) return;

    function buildValue() {
        var values = [];
        checkboxes.forEach(function (cb) {
            if (!cb.checked) return;
            if (cb.value === '__OTHER__') {
                var khacVal = txtKhac ? txtKhac.value.trim() : '';
                if (khacVal) values.push(khacVal);
            } else {
                values.push(cb.value);
            }
        });
        hidField.value = values.join(', ');
    }

    // Lắng nghe checkbox change
    checkboxes.forEach(function (cb) {
        cb.addEventListener('change', function () {
            // Ẩn/hiện textbox "Khác"
            if (cb === cbKhac && txtKhac) {
                txtKhac.style.display = cb.checked ? '' : 'none';
                if (!cb.checked) txtKhac.value = '';
                if (cb.checked) txtKhac.focus();
            }
            buildValue();
        });
    });

    // Lắng nghe textbox "Khác" input
    if (txtKhac) {
        txtKhac.addEventListener('input', function () {
            buildValue();
        });
    }
})();
