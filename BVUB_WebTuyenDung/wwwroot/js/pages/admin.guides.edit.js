// wwwroot/js/pages/admin.guides.edit.js
(function () {
    let editor;

    function getToken() {
        const f = document.querySelector('#guideEditForm input[name="__RequestVerificationToken"]');
        return f ? f.value : '';
    }

    async function initEditor() {
        const el = document.querySelector('#editor');
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
        const form = document.getElementById('guideEditForm') || document.querySelector('form');
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

    window.addEventListener('DOMContentLoaded', function () {
        initEditor();
        bindSubmit();
        bindMediaModal();
    });
})();
