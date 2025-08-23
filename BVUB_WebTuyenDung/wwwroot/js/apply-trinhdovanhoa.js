// Trình độ văn hóa: hiện/ẩn ô nhập khi chọn "Khác"
(function () {
    const otherBox = document.getElementById("txtTrinhDoKhac");

    function syncTDVH() {
        const picked = document.querySelector('input[name="DonVienChuc.TrinhDoVanHoa"]:checked');
        const isOther = picked && picked.value === "Khác";
        if (!otherBox) return;
        otherBox.style.display = isOther ? "block" : "none";
        if (!isOther) otherBox.value = "";
    }

    document.addEventListener("change", function (e) {
        if (e.target && e.target.name === "DonVienChuc.TrinhDoVanHoa") syncTDVH();
    });

    syncTDVH();
})();
