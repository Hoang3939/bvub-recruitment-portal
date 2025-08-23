// wwwroot/js/pages/admin.guides.create.js
(function () {
    let editor;

    // Lấy anti-forgery token từ form
    function getToken() {
        const f = document.querySelector('#guideCreateForm input[name="__RequestVerificationToken"]');
        return f ? f.value : '';
    }

    // Khởi tạo CKEditor Decoupled + SimpleUpload
    async function initEditor() {
        const el = document.querySelector('#editor');
        if (!el || !window.DecoupledEditor) return;

        try {
            editor = await DecoupledEditor.create(el, {
                placeholder: 'Soạn thảo hướng dẫn... (Ctrl+V để dán ảnh, kéo thả ảnh vào đây)',
                simpleUpload: {
                    uploadUrl: '/Admin/Uploads/Image',
                    withCredentials: true,
                    headers: { 'RequestVerificationToken': getToken() }
                }
            });
            document.querySelector('#toolbar-container').appendChild(editor.ui.view.toolbar.element);
            window.guideEditor = editor; // tiện dùng ở nơi khác nếu cần
        } catch (e) {
            console.error('CKEditor init fail:', e);
        }
    }

    // Trước khi submit -> copy HTML vào hidden
    function bindSubmit() {
        const form = document.getElementById('guideCreateForm');
        if (!form) return;
        form.addEventListener('submit', function () {
            try { if (editor) document.getElementById('NoiDung').value = editor.getData(); } catch { }
        });
    }

    // Modal “Thư viện ảnh”
    function bindMediaModal() {
        const mediaModal = document.getElementById('mediaModal');
        if (!mediaModal) return;

        mediaModal.addEventListener('show.bs.modal', async () => {
            const grid = document.getElementById('mediaGrid');
            grid.innerHTML = '<div class="text-muted">Đang tải...</div>';
            try {
                const resp = await fetch('/Admin/Uploads/List', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                if (!resp.ok) throw new Error('HTTP ' + resp.status);
                const files = await resp.json();
                grid.innerHTML = '';
                (files || []).forEach(url => {
                    const col = document.createElement('div');
                    col.className = 'col-3';
                    col.innerHTML = `<img src="${url}" style="width:100%;height:120px;object-fit:cover;border-radius:8px;cursor:pointer" title="Chèn ảnh">`;
                    col.querySelector('img').onclick = () => {
                        if (!editor) return;
                        editor.model.change(writer => {
                            const img = writer.createElement('imageBlock', { src: url });
                            editor.model.insertContent(img, editor.model.document.selection);
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

    // Boot
    window.addEventListener('DOMContentLoaded', function () {
        initEditor();
        bindSubmit();
        bindMediaModal();
    });
})();
