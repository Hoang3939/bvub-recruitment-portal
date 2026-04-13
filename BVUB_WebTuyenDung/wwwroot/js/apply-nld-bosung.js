/**
 * apply-nld-bosung.js
 * Logic ẩn/hiện cho section "Thông tin bổ sung" trong form NLD:
 *   - Dân tộc: dropdown → hidden field, textbox "Khác"
 *   - Tôn giáo: dropdown → hidden field, textbox "Khác"
 *   - Đảng viên: radio → hidden field, ẩn/hiện ngày vào Đảng + ngày chính thức
 */
(function () {
    'use strict';

    // === Dân tộc ===
    var selDanToc = document.getElementById('selDanToc');
    var hidDanToc = document.getElementById('hidDanToc');
    var txtDanTocKhac = document.getElementById('txtDanTocKhac');

    if (selDanToc && hidDanToc) {
        selDanToc.addEventListener('change', function () {
            var val = selDanToc.value;
            if (val === '__OTHER__') {
                txtDanTocKhac.style.display = '';
                txtDanTocKhac.focus();
                hidDanToc.value = '__OTHER__';
            } else {
                txtDanTocKhac.style.display = 'none';
                txtDanTocKhac.value = '';
                hidDanToc.value = val;
            }
        });

        // Khi nhập textbox "Khác" → cập nhật hidden nếu đang chọn Khác
        if (txtDanTocKhac) {
            txtDanTocKhac.addEventListener('input', function () {
                if (selDanToc.value === '__OTHER__') {
                    hidDanToc.value = txtDanTocKhac.value.trim() || '__OTHER__';
                }
            });
        }
    }

    // === Tôn giáo ===
    var selTonGiao = document.getElementById('selTonGiao');
    var hidTonGiao = document.getElementById('hidTonGiao');
    var txtTonGiaoKhac = document.getElementById('txtTonGiaoKhac');

    if (selTonGiao && hidTonGiao) {
        selTonGiao.addEventListener('change', function () {
            var val = selTonGiao.value;
            if (val === '__OTHER__') {
                txtTonGiaoKhac.style.display = '';
                txtTonGiaoKhac.focus();
                hidTonGiao.value = '__OTHER__';
            } else {
                txtTonGiaoKhac.style.display = 'none';
                txtTonGiaoKhac.value = '';
                hidTonGiao.value = val;
            }
        });

        if (txtTonGiaoKhac) {
            txtTonGiaoKhac.addEventListener('input', function () {
                if (selTonGiao.value === '__OTHER__') {
                    hidTonGiao.value = txtTonGiaoKhac.value.trim() || '__OTHER__';
                }
            });
        }
    }

    // === Đảng viên ===
    var hidDangVien = document.getElementById('hidDangVien');
    var dangVienDates = document.getElementById('dangVienDates');
    var rdCo = document.getElementById('rdDangVienCo');
    var rdKhong = document.getElementById('rdDangVienKhong');

    function toggleDangVien() {
        if (!hidDangVien || !dangVienDates) return;
        var isDangVien = rdCo && rdCo.checked;
        hidDangVien.value = isDangVien ? 'true' : 'false';
        dangVienDates.style.display = isDangVien ? '' : 'none';

        // Xóa giá trị ngày nếu không phải Đảng viên
        if (!isDangVien) {
            var inputs = dangVienDates.querySelectorAll('input[type="text"]');
            for (var i = 0; i < inputs.length; i++) {
                inputs[i].value = '';
            }
        }
    }

    if (rdCo) rdCo.addEventListener('change', toggleDangVien);
    if (rdKhong) rdKhong.addEventListener('change', toggleDangVien);

    // Init state
    toggleDangVien();
})();
