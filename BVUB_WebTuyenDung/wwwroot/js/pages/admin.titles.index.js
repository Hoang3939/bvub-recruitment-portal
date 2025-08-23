// wwwroot/js/pages/admin.titles.index.js
(() => {
    // ===== Helper: show toast nếu có =====
    const toast = document.getElementById("toast");
    if (toast) {
        setTimeout(() => {
            toast.classList.add("show");
            setTimeout(() => toast.classList.remove("show"), 2200);
        }, 50);
    }

    // ====== Lấy config URL ======
    const cfg = document.getElementById("admin-titles-index-config");
    const detailsUrl = cfg?.dataset?.detailsUrl || "/Admin/Titles/DetailsPartial";

    // ====== Modal chi tiết ======
    const detailModal = document.getElementById("titleDetailModal");
    const detailWrap = document.getElementById("titleDetailContainer");

    function openModal() {
        detailModal.classList.add("show");
        document.body.style.overflow = "hidden";
    }
    function closeModal() {
        detailModal.classList.remove("show");
        document.body.style.overflow = "";
    }

    detailModal
        .querySelectorAll(".modal-close")
        .forEach((el) => el.addEventListener("click", closeModal));

    detailModal.addEventListener("click", (e) => {
        if (e.target === detailModal) closeModal();
    });

    // Click vào hàng để xem chi tiết (bỏ qua các phần tử tương tác)
    document.addEventListener("click", async (e) => {
        const inter = e.target.closest(
            "button, a, .btn, .btn-icon, svg, path, input, select, textarea"
        );
        if (inter) return;

        const row = e.target.closest('tr.row-click[data-id]');
        if (!row) return;

        const id = row.getAttribute('data-id');
        if (!id) return;

        detailWrap.innerHTML = 'Đang tải...';
        try {
            const url = `${detailsUrl}?id=${encodeURIComponent(id)}`;
            const resp = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" },
            });
            detailWrap.innerHTML = resp.ok ? await resp.text() : "Không tải được dữ liệu.";
        } catch {
            detailWrap.innerHTML = "Có lỗi khi tải dữ liệu.";
        }
        openModal();
    });

    // ====== Confirm delete ======
    const delModal = document.getElementById("confirmDelete");
    const btnDelNo = document.getElementById("btnDelCancel");
    const btnDelYes = document.getElementById("btnDelYes");
    let targetFormSel = null;

    function openDel() {
        delModal.classList.add("show");
        document.body.style.overflow = "hidden";
    }
    function closeDel() {
        delModal.classList.remove("show");
        document.body.style.overflow = "";
        targetFormSel = null;
    }

    delModal
        .querySelectorAll(".modal-close")
        .forEach((el) => el.addEventListener("click", closeDel));
    btnDelNo?.addEventListener("click", closeDel);
    delModal.addEventListener("click", (e) => {
        if (e.target === delModal) closeDel();
    });

    // Bấm nút thùng rác → mở confirm
    document.addEventListener("click", (e) => {
        const btn = e.target.closest(".btn-delete");
        if (!btn) return;
        targetFormSel = btn.getAttribute("data-form"); // ví dụ "#del-5"
        openDel();
    });

    // Đồng ý xóa → submit form tương ứng (đã có AntiForgery trong form)
    btnDelYes?.addEventListener("click", () => {
        if (!targetFormSel) return closeDel();
        const frm = document.querySelector(targetFormSel);
        if (frm) frm.submit();
        closeDel();
    });
})();
