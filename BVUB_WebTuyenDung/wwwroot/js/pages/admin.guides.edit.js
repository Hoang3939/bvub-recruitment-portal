// wwwroot/js/pages/admin.guides.edit.js
(function () {
    let editor;

    function getToken() {
        const f = document.querySelector('#guideEditForm input[name="__RequestVerificationToken"]');
        return f ? f.value : '';
    }

    async function initEditor() {
        const el = document.getElementById('editor');
        if (!el || !window.DecoupledEditor) return;

        try {
            editor = await DecoupledEditor.create(el, {
                simpleUpload: {
                    uploadUrl: '/Admin/Uploads/Image',
                    withCredentials: true,
                    headers: { 'RequestVerificationToken': getToken() }
                }
            });
            document.querySelector('#toolbar-container').appendChild(editor.ui.view.toolbar.element);
            window.guideEditor = editor;
        } catch (e) {
            console.error('CKEditor init fail:', e);
        }
    }

    function bindSubmit() {
        const form = document.getElementById('guideEditForm');
        if (!form) return;
        form.addEventListener('submit', function () {
            try { if (editor) document.getElementById('NoiDung').value = editor.getData(); } catch { }
        });
    }

    function bindMediaModal() {
        const mediaModal = document.getElementById('mediaModal');
        if (!mediaModal) return;

        mediaModal.addEventListener('show.bs.modal', async () => {
            const grid = document.getElementById('mediaGrid');
            grid.innerHTML = '<div class="text-muted">Đang tải...</div>';
            try {
                const resp = await fetch('/Admin/Uploads/List', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                const files = await resp.json();
                grid.innerHTML = '';
                (files || []).forEach(url => {
                    const col = document.createElement('div');
                    col.className = 'col-3';
                    col.innerHTML = `<img src="${url}" style="width:100%;height:120px;object-fit:cover;border-radius:8px;cursor:pointer" title="Chèn ảnh">`;
                    col.querySelector('img').onclick = () => {
                        if (!editor) return;
                        editor.model.change(writer => {
                            const imageElement = writer.createElement('imageBlock', { src: url });
                            editor.model.insertContent(imageElement, editor.model.document.selection);
                        });
                        bootstrap.Modal.getInstance(mediaModal)?.hide();
                    };
                    grid.appendChild(col);
                });
                if (!files || files.length === 0) grid.innerHTML = '<div class="text-muted">Chưa có ảnh nào trong thư viện.</div>';
            } catch (err) {
                console.error(err);
                grid.innerHTML = '<div class="text-danger">Không tải được thư viện ảnh.</div>';
            }
        });
    }

    function bindConfirmAndToast() {
        const form = document.getElementById('guideEditForm');
        const modal = document.getElementById('confirmSaveModal');
        const toast = document.getElementById('adminToast');
        if (!form || !modal) return;

        const btnCancel = modal.querySelector('.btn-cancel');
        const btnOk = modal.querySelector('.btn-ok');
        const btnClose = modal.querySelector('.modal-close');

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            modal.classList.add('show');
            document.body.style.overflow = 'hidden';
        });

        const closeModal = () => {
            modal.classList.remove('show');
            document.body.style.overflow = '';
        };
        btnCancel.onclick = closeModal;
        btnClose.onclick = closeModal;
        modal.addEventListener('click', e => { if (e.target === modal) closeModal(); });

        btnOk.onclick = () => {
            try { if (editor) document.getElementById('NoiDung').value = editor.getData(); } catch { }
            closeModal();
            form.submit();
        };

        const ok = document.body.dataset.toastOk;
        const err = document.body.dataset.toastErr;
        if (ok) showToast(ok, 'success');
        if (err) showToast(err, 'error');

        function showToast(message, type = 'success') {
            if (!toast) return;
            toast.textContent = message;
            toast.className = `toast ${type}`;
            setTimeout(() => toast.classList.add('show'), 10);
            setTimeout(() => toast.classList.remove('show'), 3800);
            setTimeout(() => (toast.className = 'toast'), 4400);
        }
    }

    window.addEventListener('DOMContentLoaded', function () {
        initEditor();
        bindSubmit();
        bindMediaModal();
        bindConfirmAndToast();
    });
})();
