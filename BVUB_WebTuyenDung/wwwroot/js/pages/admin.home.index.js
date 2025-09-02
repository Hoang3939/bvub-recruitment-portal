(function () {
    function el(id) { return document.getElementById(id); }
    function parseJson(str, fallback) { try { return JSON.parse(str); } catch { return fallback; } }

    // <-- Line chart -->
    function makeLine(labels, data) {
        const ctx = el('line30');
        if (!ctx || typeof Chart === 'undefined') return;
        new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Ứng viên',
                    data,
                    tension: 0.35,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
                plugins: { legend: { display: false } }
            }
        });
    }

    // <-- Gender pie -->
    function makeGenderPie(female, male) {
        const ctx = el('genderPie');
        if (!ctx || typeof Chart === 'undefined') return;
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Nữ', 'Nam'],
                datasets: [{ data: [female, male] }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } },
                cutout: '55%'
            }
        });
    }

    // <-- Age bar -->
    function makeAgeBar(buckets) {
        const ctx = el('ageBar');
        if (!ctx || typeof Chart === 'undefined') return;
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: buckets.map(b => b.Label ?? b.label ?? ''),
                datasets: [{ label: 'Số lượng', data: buckets.map(b => b.Count ?? b.count ?? 0) }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
                plugins: { legend: { display: false } }
            }
        });
    }

    // <-- Init -->
    window.addEventListener('DOMContentLoaded', function () {
        const cfg = el('admin-home-index-config');
        if (!cfg) return;

        const series = parseJson(cfg.dataset.series || '[]', []);
        const labels = series.map(x => x.Day ?? x.day ?? '');
        const counts = series.map(x => x.Count ?? x.count ?? 0);
        const female = Number(cfg.dataset.female || 0);
        const male = Number(cfg.dataset.male || 0);
        const buckets = parseJson(cfg.dataset.buckets || '[]', []);

        makeLine(labels, counts);
        makeGenderPie(female, male);
        makeAgeBar(buckets);
    });
})();
