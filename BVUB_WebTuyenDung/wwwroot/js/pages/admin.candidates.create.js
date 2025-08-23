(function () {
    const $ = window.jQuery;
    function byId(id) { return document.getElementById(id); }
    function val(el) { return (el && el.value || "").trim(); }

    function syncTinhTrang(trigger) {
        const sel = byId('TinhTrangSelect'), wrap = byId('TinhTrangOtherWrap');
        const other = byId('TinhTrangOther'), hidden = byId('TinhTrangSucKhoe');
        if (!sel || !hidden) return;
        if (sel.value === '__OTHER__') { if (wrap) wrap.style.display = ''; hidden.value = val(other); }
        else { if (wrap) wrap.style.display = 'none'; hidden.value = sel.value || ''; }
        if (trigger && window.jQuery && $.fn.valid) {
            $(hidden).valid();
            if (wrap && wrap.style.display !== 'none' && other) $(other).valid();
        }
    }

    function initValidation() {
        if (!window.jQuery || !$.validator) return;
        $.validator.setDefaults({
            ignore: [], onkeyup: el => $(el).valid(), onfocusout: el => $(el).valid(),
            highlight: el => el.classList.add('is-invalid'),
            unhighlight: el => el.classList.remove('is-invalid')
        });
        const $form = $('#frmCreateUv');
        $form.removeData('validator').removeData('unobtrusiveValidation');
        if ($.validator.unobtrusive) $.validator.unobtrusive.parse($form);
    }

    function clampDatesToToday() {
        const today = new Date().toISOString().slice(0, 10);
        const ns = byId('NgaySinh'), nc = byId('NgayCapCCCD');
        if (ns) ns.max = today; if (nc) nc.max = today;
    }

    function initPresetValue() {
        const hidden = byId('TinhTrangSucKhoe'), sel = byId('TinhTrangSelect');
        const other = byId('TinhTrangOther'), wrap = byId('TinhTrangOtherWrap');
        const presets = ["Loại I", "Loại II", "Loại III"]; const current = val(hidden);
        if (!sel || !wrap) return;
        if (!current) { sel.value = ''; wrap.style.display = 'none'; }
        else if (presets.includes(current)) { sel.value = current; wrap.style.display = 'none'; }
        else { sel.value = '__OTHER__'; wrap.style.display = ''; if (other) other.value = current; }
    }

    document.addEventListener('DOMContentLoaded', () => {
        const sel = byId('TinhTrangSelect'), other = byId('TinhTrangOther'), form = byId('frmCreateUv');
        sel && sel.addEventListener('change', () => syncTinhTrang(true));
        other && other.addEventListener('input', () => syncTinhTrang(true));
        form && form.addEventListener('submit', () => syncTinhTrang(true));
        clampDatesToToday(); initValidation(); initPresetValue(); syncTinhTrang(false);
    });
})();
