// wwwroot/js/pages/admin.departments.form.js
(function () {
    function $id(id) { return document.getElementById(id); }
    function text(el) { return (el && el.value || "").trim(); }

    function getConfig() {
        const el = document.getElementById('admin-dept-form-config');
        if (!el) return null;
        let pre = [];
        try { pre = JSON.parse(el.dataset.preselected || '[]'); } catch { pre = []; }
        return {
            preselected: Array.isArray(pre) ? pre : [],
            urlByTitle: el.dataset.urlByTitle || '',
            urlByIds: el.dataset.urlByIds || ''
        };
    }

    function renderSelected(selected, listEl, hiddenEl) {
        listEl.innerHTML = '';
        hiddenEl.innerHTML = '';
        if (selected.size === 0) {
            listEl.innerHTML = '<div class="muted">Chưa có vị trí nào được chọn.</div>';
            return;
        }
        for (const [id, label] of selected.entries()) {
            const chip = document.createElement('div');
            chip.className = 'chip';
            chip.dataset.id = id;
            chip.innerHTML = `<span>${label}</span><button type="button" class="chip-x" aria-label="Bỏ chọn">&times;</button>`;
            listEl.appendChild(chip);

            const hid = document.createElement('input');
            hid.type = 'hidden';
            hid.name = 'SelectedViTriIds';
            hid.value = id;
            hiddenEl.appendChild(hid);
        }
    }

    function syncChecksInGrid(selected, grid) {
        grid.querySelectorAll('input[type="checkbox"][data-id]').forEach(cb => {
            const id = Number(cb.getAttribute('data-id'));
            cb.checked = selected.has(id);
        });
    }

    async function loadPositionsByTitle(cfg, titleId, selected, grid) {
        grid.innerHTML = '<div class="pos-loading">Đang tải danh sách vị trí…</div>';
        if (!titleId) {
            grid.innerHTML = '<div class="muted">Chọn chức danh để hiển thị vị trí.</div>';
            return;
        }
        try {
            const url = cfg.urlByTitle + '?chucDanhId=' + encodeURIComponent(titleId);
            const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const data = await resp.json();
            if (!data.items || data.items.length === 0) {
                grid.innerHTML = '<div class="muted">Chức danh này chưa có vị trí khả dụng.</div>';
                return;
            }
            const html = data.items.map(it => {
                const id = Number(it.value);
                const checked = selected.has(id) ? 'checked' : '';
                const safe = (it.text || '').replace(/"/g, '&quot;');
                return `<label class="pos-item">
                  <input type="checkbox" data-id="${id}" data-text="${safe}" ${checked}/>
                  <span>${safe}</span>
                </label>`;
            }).join('');
            grid.innerHTML = html;
        } catch {
            grid.innerHTML = '<div class="muted">Không tải được danh sách vị trí.</div>';
        }
    }

    async function hydratePreSelected(cfg, preSel, selected, listEl, grid, hiddenEl) {
        if (!preSel || preSel.length === 0) {
            renderSelected(selected, listEl, hiddenEl);
            return;
        }
        try {
            const url = cfg.urlByIds + '?ids=' + preSel.join(',');
            const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const data = await resp.json();
            const found = new Map((data.items || []).map(it => [Number(it.value), it.text]));
            preSel.forEach(id => {
                const n = Number(id);
                selected.set(n, found.get(n) || ('#' + n));
            });
        } finally {
            renderSelected(selected, listEl, hiddenEl);
            syncChecksInGrid(selected, grid);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        const cfg = getConfig();
        if (!cfg) return;

        const ddlTitle = $id('ddlTitle');
        const grid = $id('posGrid');
        const selList = $id('selectedList');
        const selHidden = $id('selectedHidden');

        if (!ddlTitle || !grid || !selList || !selHidden) return;

        const selected = new Map();

        // Events
        grid.addEventListener('change', e => {
            const cb = e.target.closest('input[type="checkbox"][data-id]');
            if (!cb) return;
            const id = Number(cb.getAttribute('data-id'));
            const label = cb.getAttribute('data-text') || ('#' + id);
            if (cb.checked) selected.set(id, label);
            else selected.delete(id);
            renderSelected(selected, selList, selHidden);
        });

        selList.addEventListener('click', e => {
            const btn = e.target.closest('.chip-x');
            if (!btn) return;
            const chip = btn.closest('.chip');
            const id = Number(chip.dataset.id);
            selected.delete(id);
            renderSelected(selected, selList, selHidden);
            grid.querySelectorAll(`input[type="checkbox"][data-id="${id}"]`)
                .forEach(cb => cb.checked = false);
        });

        ddlTitle.addEventListener('change', () => loadPositionsByTitle(cfg, ddlTitle.value, selected, grid));

        // First render
        hydratePreSelected(cfg, cfg.preselected, selected, selList, grid, selHidden);
    });
})();
