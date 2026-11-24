// Codeownership: Florian Pausch

/* 
   STATISTIK UND FORTSCHRITT
══════════ */

function initSidebar() {
    const sidebar   = document.querySelector('.sidebar');
    if (!sidebar) return;

    const pct       = parseInt(sidebar.dataset.pct);
    const completed = parseInt(sidebar.dataset.rated);
    const total     = parseInt(sidebar.dataset.total);
    const doneText = sidebar.dataset.doneText ?? sidebar.dataset.donePt + ' PT';
    const openText = sidebar.dataset.openText ?? sidebar.dataset.openPt + ' PT';
    const bufferText = sidebar.dataset.bufferText ?? sidebar.dataset.bufferPt + ' PT';
    const bufferToneClass = sidebar.dataset.bufferToneClass ?? (sidebar.dataset.bufferNeg === 'true' ? 'negative' : 'positive');
    const attendedText = sidebar.dataset.attendedText ?? sidebar.dataset.attendedPt + ' PT';
    const velocityText = sidebar.dataset.velocityText ?? sidebar.dataset.velocity + '%';
    const velocityToneClass = sidebar.dataset.velocityToneClass ?? (parseInt(sidebar.dataset.velocity) >= 100 ? 'positive' : 'negative');
    const forecastText = sidebar.dataset.forecastText ?? sidebar.dataset.forecastBufferPt + ' PT';
    const forecastToneClass = sidebar.dataset.forecastToneClass ?? (sidebar.dataset.forecastBufferNeg === 'true' ? 'negative' : 'positive');

    // SVG-Ring — einzige Berechnung die im Frontend verbleibt
    const circumference = 2 * Math.PI * 22;
    const ring = document.getElementById('progress-ring');
    if (ring) {
        ring.style.strokeDasharray  = circumference;
        ring.style.strokeDashoffset = total > 0
            ? circumference * (1 - completed / total)
            : circumference;
    }

    const progressPct   = document.getElementById('progress-pct');
    const progressLabel = document.getElementById('progress-label');
    const progressBar   = document.getElementById('progress-bar');
    if (progressPct)   progressPct.textContent   = pct + '%';
    if (progressLabel) progressLabel.textContent = `${completed} von ${total} Lektionen`;
    if (progressBar)   progressBar.style.width   = pct + '%';

    const statGeschafft = document.getElementById('stat-geschafft');
    const statOffen     = document.getElementById('stat-offen');
    const statPuffer    = document.getElementById('stat-puffer');
    const statAnwesend  = document.getElementById('stat-anwesend');
    const statGeschwindigkeit = document.getElementById('stat-geschwindigkeit');
    const statPrognose = document.getElementById('stat-prognose');
    if (statAnwesend)  statAnwesend.textContent = attendedText;
    if (statGeschafft) statGeschafft.textContent = doneText;
    if (statOffen)     statOffen.textContent     = openText;
    if (statPuffer) {
        statPuffer.textContent = bufferText;
        statPuffer.className   = 'stat-row-val ' + toneToSidebarClass(bufferToneClass);
    }
    if (statGeschwindigkeit) {
        statGeschwindigkeit.textContent = velocityText;
        statGeschwindigkeit.className = 'stat-row-val ' + toneToSidebarClass(velocityToneClass);
    }
    if (statPrognose) {
        statPrognose.textContent = forecastText;
        statPrognose.className = 'stat-row-val ' + toneToSidebarClass(forecastToneClass);
    }

    const navPuffer = document.getElementById('nav-puffer');
    if (navPuffer) {
        navPuffer.textContent = sidebar.dataset.navBufferText ?? bufferText;
        navPuffer.className = sidebar.dataset.navBufferClass ?? 'puffer-val';
    }
}

function updateDashboardStats(stats) {
    if (!stats) return;

    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        sidebar.dataset.total = stats.total;
        sidebar.dataset.rated = stats.rated;
        sidebar.dataset.pct = stats.pct;
        sidebar.dataset.attendedPt = stats.attendedPt;
        sidebar.dataset.donePt = stats.donePt;
        sidebar.dataset.openPt = stats.openPt;
        sidebar.dataset.bufferPt = stats.bufferPt;
        sidebar.dataset.bufferNeg = stats.bufferNeg ? 'true' : 'false';
        sidebar.dataset.velocity = stats.velocity;
        sidebar.dataset.forecastBufferPt = stats.forecastBufferPt;
        sidebar.dataset.forecastBufferNeg = stats.forecastBufferNeg ? 'true' : 'false';
        sidebar.dataset.attendedText = stats.attendedText;
        sidebar.dataset.doneText = stats.doneText;
        sidebar.dataset.openText = stats.openText;
        sidebar.dataset.bufferText = stats.bufferText;
        sidebar.dataset.bufferToneClass = stats.bufferToneClass;
        sidebar.dataset.velocityText = stats.velocityText;
        sidebar.dataset.velocityToneClass = stats.velocityToneClass;
        sidebar.dataset.forecastText = stats.forecastText;
        sidebar.dataset.forecastToneClass = stats.forecastToneClass;
        sidebar.dataset.navBufferText = stats.navBufferText;
        sidebar.dataset.navBufferClass = stats.navBufferClass;
    }

    const countByStatus = stats.countByStatus ?? {};
    const allCount = document.getElementById('fc-all');
    if (allCount) allCount.textContent = stats.total;
    Object.entries(countByStatus).forEach(([status, count]) => {
        const el = document.getElementById(`fc-${status}`);
        if (el) el.textContent = count;
    });

    updateHeaderSummary(stats);
    updateStatisticsView(stats);
    initSidebar();
}

function updateHeaderSummary(stats) {
    const summary = document.getElementById('header-summary');
    if (summary) summary.textContent = stats.headerSummaryText;

    const buffer = document.getElementById('header-buffer');
    if (buffer) {
        buffer.textContent = stats.headerBufferText;
        buffer.className = stats.headerBufferClass;
    }

    const attentionEl = document.getElementById('header-attention');
    if (attentionEl) attentionEl.textContent = stats.attentionCount;

    const reviewEl = document.getElementById('header-review');
    if (reviewEl) reviewEl.textContent = stats.reviewCount;
}

function updateStatisticsView(stats) {
    setText('stats-attended', stats.attendedText);
    setText('stats-done', stats.doneText);
    setText('stats-open', stats.openText);
    setText('stats-buffer', stats.bufferText);
    setClass('stats-buffer', stats.bufferToneClass);
    setText('stats-velocity', stats.velocityText);
    setClass('stats-velocity', stats.velocityToneClass);
    setText('stats-forecast', stats.forecastText);
    setClass('stats-forecast', stats.forecastToneClass);
    setText('stats-attended-bar-value', stats.attendedText);
    setText('stats-done-bar-value', stats.doneText);
    setText('stats-buffer-inline', stats.bufferText);
    setClass('stats-buffer-inline', stats.bufferToneClass);

    setWidth('stats-attended-bar', stats.attendedWidthCss);
    setWidth('stats-done-bar', stats.doneWidthCss);
    setWidth('stats-done-stack', stats.doneShareCss);
    setWidth('stats-open-stack', stats.openShareCss);
    setText('stats-done-share', stats.doneShareText);
    setText('stats-open-share', stats.openShareText);

    Object.entries(stats.countByStatus ?? {}).forEach(([status, count]) => {
        setText(`stats-status-${status}`, count);
    });
}

function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value;
}

function setClass(id, className) {
    const el = document.getElementById(id);
    if (el) el.className = className;
}

function setWidth(id, width) {
    const el = document.getElementById(id);
    if (el) el.style.width = `${Math.max(0, Math.min(100, width))}%`;
}

function toneToSidebarClass(tone) {
    return tone === 'negative' ? 'red' : 'green';
}
