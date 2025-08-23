// Tình trạng sức khỏe: hiển thị ô "Khác" + public setter
(function () {
    const radios = document.querySelectorAll(".js-sk");
    const hid = document.getElementById("hidSK");
    const txt = document.getElementById("txtSkKhac");
    const rOther = document.getElementById("skKhac");
    const KNOWN = ["Loại I", "Loại II", "Loại III"];

    function reflectHiddenToUI() {
        const val = (hid?.value || "").trim();
        if (!txt || !hid) return;

        if (!val) {
            txt.style.display = "none"; txt.value = "";
            radios.forEach(r => r.checked = false);
            return;
        }
        if (KNOWN.includes(val)) {
            txt.style.display = "none"; txt.value = "";
            radios.forEach(r => r.checked = (r.value === val));
        } else {
            if (rOther) rOther.checked = true;
            txt.style.display = "block";
            txt.value = val;
        }
    }

    radios.forEach(r => r.addEventListener("change", function () {
        if (!hid || !txt) return;
        if (this.value === "__OTHER__") {
            txt.style.display = "block";
            hid.value = txt.value.trim();
        } else {
            txt.style.display = "none";
            txt.value = "";
            hid.value = this.value;
        }
    }));

    if (txt && rOther) {
        txt.addEventListener("input", function () {
            if (rOther.checked && hid) hid.value = this.value.trim();
        });
    }

    // public cho ajax fill
    window.setTinhTrangSucKhoe = function (value) {
        if (!hid) return;
        hid.value = (value || "").trim();
        reflectHiddenToUI();
    };

    reflectHiddenToUI();
})();