(function () {
    // ===== Helpers =====
    const $ = (sel, root = document) => root.querySelector(sel);
    const cfgEl = $('#reports-config');
    const urls = {
        summary: cfgEl?.dataset.summaryUrl || '',
        recSeries: cfgEl?.dataset.recSeriesUrl || '',
        recTypeMonthly: cfgEl?.dataset.recTypeMonthlyUrl || '',
        candSeries: cfgEl?.dataset.candSeriesUrl || '',
        candBreakdown: cfgEl?.dataset.candBreakdownUrl || ''
    };

    const sel = $('#rangeSelect');
    const custom = $('#customRange');
    const fromInp = $('#from');
    const toInp = $('#to');
    const btn = $('#btnApply');

    const viDate = (d) => new Date(d).toLocaleDateString('vi-VN');

    function dateOnly(d) {
        // local 00:00 for today if Date provided; else parse yyyy-MM-dd
        if (d instanceof Date) {
            return new Date(d.getFullYear(), d.getMonth(), d.getDate());
        }
        if (typeof d === 'string') {
            const [y, m, dd] = d.split('-').map(Number);
            return new Date(y, (m || 1) - 1, dd || 1);
        }
        return new Date();
    }
    function toYmd(d) {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        return `${y}-${m}-${dd}`;
    }

    function setPreset(val) {
        const end = dateOnly(new Date());
        let start = null;

        switch (val) {
            case '7': start = new Date(end); start.setDate(end.getDate() - 6); break;
            case '30': start = new Date(end); start.setDate(end.getDate() - 29); break;
            case 'quarter': {
                const qStartMonth = Math.floor(end.getMonth() / 3) * 3;
                start = new Date(end.getFullYear(), qStartMonth, 1);
                break;
            }
            case 'year': start = new Date(end.getFullYear(), 0, 1); break;
        }
        if (start) {
            fromInp.value = toYmd(start);
            toInp.value = toYmd(end);
            toInp.min = fromInp.value;
            fromInp.max = toInp.value;
        }
    }

    function onRangeChange() {
        const useCustom = sel.value === 'custom';
        custom.classList.toggle('show', useCustom);
        custom.setAttribute('aria-hidden', useCustom ? 'false' : 'true');
        if (!useCustom) setPreset(sel.value);
    }

    fromInp?.addEventListener('change', () => { if (toInp && fromInp) toInp.min = fromInp.value; });
    toInp?.addEventListener('change', () => { if (toInp && fromInp) fromInp.max = toInp.value; });

    sel?.addEventListener('change', onRangeChange);
    onRangeChange(); // init default

    // ===== Charts =====
    let recChart, recTypeChart, candChart, candDonut;

    function destroyChart(c) {
        if (c && typeof c.destroy === 'function') try { c.destroy(); } catch { }
    }

    async function safeJson(url) {
        const resp = await fetch(url);
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        return await resp.json();
    }

    function qs() {
        const p = new URLSearchParams({ from: fromInp.value, to: toInp.value });
        return '?' + p.toString();
    }

    // KPI Summary
    async function loadSummary() {
        const j = await safeJson(urls.summary + qs());
        $('#c-pending').textContent = j.pendingApplications ?? 0;
        $('#c-active').textContent = j.activeRecruitments ?? 0;
        $('#c-soon').textContent = j.expiringSoonRecruitments ?? 0;
        $('#c-total-rec').textContent = j.totalRecruitments ?? 0;
        $('#c-vc').textContent = j.byTypeVC ?? 0;
        $('#c-hd').textContent = j.byTypeHD ?? 0;
        $('#c-new-cands').textContent = j.newCandidates ?? 0;

        const box = $('#top-exp'); box.innerHTML = '';
        (j.expiringTop || []).forEach(x => {
            const d = document.createElement('div');
            d.className = 'd-flex justify-content-between align-items-center border-bottom py-1';
            const han = x.han ? viDate(x.han) : '';
            d.innerHTML = `
        <div>
          <div class="fw-semibold">${x.tieuDe}</div>
          <div class="text-muted">Loại: ${x.loai || ''}</div>
        </div>
        <div class="text-end small">
          Hạn: ${han}<br/>còn <b>${x.daysLeft}</b> ngày
        </div>`;
            box.appendChild(d);
        });
        if (!box.innerHTML) box.innerHTML = '<div class="text-muted">Không có tin sắp hết hạn.</div>';
    }

    // Line: Recruitment by day
    async function loadRecChart() {
        const j = await safeJson(urls.recSeries + qs());
        destroyChart(recChart);
        recChart = new Chart($('#recChart'), {
            type: 'line',
            data: { labels: j.labels, datasets: [{ label: 'Tin TD', data: j.data, tension: .3, fill: false }] },
            options: { responsive: true, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } }
        });
    }

    // Stacked Bar: VC/HD by month
    async function loadRecTypeChart() {
        const j = await safeJson(urls.recTypeMonthly + qs());
        destroyChart(recTypeChart);
        recTypeChart = new Chart($('#recTypeChart'), {
            type: 'bar',
            data: {
                labels: j.labels,
                datasets: [
                    { label: 'Viên chức', data: j.a, stack: 't' },
                    { label: 'HĐ lao động', data: j.b, stack: 't' }
                ]
            },
            options: {
                responsive: true,
                scales: {
                    x: { stacked: true },
                    y: { stacked: true, beginAtZero: true, ticks: { precision: 0 } }
                }
            }
        });
    }

    // Line: Candidates by day (total/vc/hd)
    async function loadCandChart() {
        const j = await safeJson(urls.candSeries + qs());
        destroyChart(candChart);
        candChart = new Chart($('#candChart'), {
            type: 'line',
            data: {
                labels: j.labels,
                datasets: [
                    { label: 'Tổng', data: j.total, tension: .3 },
                    { label: 'Viên chức', data: j.vc, tension: .3 },
                    { label: 'HĐ lao động', data: j.hd, tension: .3 }
                ]
            },
            options: { responsive: true, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } }
        });
    }

    // Donut: Candidate breakdown
    async function loadCandBreakdown() {
        const j = await safeJson(urls.candBreakdown + qs());
        destroyChart(candDonut);
        candDonut = new Chart($('#candBreakdown'), {
            type: 'doughnut',
            data: {
                labels: ['Chờ duyệt', 'Đã duyệt', 'Đã huỷ'],
                datasets: [{ data: [j.pending || 0, j.approved || 0, j.cancelled || 0] }]
            },
            options: { responsive: true, plugins: { legend: { position: 'bottom' } } }
        });
    }

    async function applyAll() {
        // loading state
        btn.disabled = true;
        const orig = btn.textContent;
        btn.textContent = 'Đang tải…';

        try {
            await Promise.all([
                loadSummary(),
                loadRecChart(),
                loadRecTypeChart(),
                loadCandChart(),
                loadCandBreakdown()
            ]);
        } catch (e) {
            console.error(e);
            alert('Không tải được dữ liệu báo cáo. Vui lòng thử lại.');
        } finally {
            btn.disabled = false;
            btn.textContent = orig;
        }
    }

    btn?.addEventListener('click', applyAll);

    // auto run lần đầu
    applyAll();
})();
