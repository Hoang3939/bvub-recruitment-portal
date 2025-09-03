(function () {
    // Chỉ chạy nếu có radio Loại hình đào tạo trên trang
    const radios = document.querySelectorAll("input[name='DonVienChuc.LoaiHinhDaoTao']");
    if (!radios || radios.length === 0) return;

    const otherInput = document.querySelector(".js-loaihinh-khac-input");
    if (!otherInput) return;

    function showOther(show) {
        otherInput.style.display = show ? "block" : "none";
        if (!show) otherInput.value = "";
    }

    // Khi đổi lựa chọn
    document.addEventListener("change", function (e) {
        if (!e.target.matches("input[name='DonVienChuc.LoaiHinhDaoTao']")) return;
        const isOther = e.target.value === "Khác";
        showOther(isOther);
    });

    // Preload (khi quay lại trang hoặc có value từ server)
    (function preload() {
        // Nếu đã có radio được chọn và là "Khác" => mở ô nhập
        const picked = document.querySelector("input[name='DonVienChuc.LoaiHinhDaoTao']:checked");
        if (picked && picked.value === "Khác") {
            showOther(true);
            return;
        }
    })();
})();