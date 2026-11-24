
/* ═══════════════════════════════════════════════════════
   trainee-dashboard.js
   Verantwortung: PT-Info-Popover und gemeinsame UI-Timer.
═══════════════════════════════════════════════════════ */


/* ═══════════════════════════════════════════════════════
   UI-TIMER
═══════════════════════════════════════════════════════ */
let errorTimer;
let toastTimer;

function initPtInfoPopover() {
    const btn = document.querySelector('.pt-info-btn');
    const overlay = document.getElementById('pt-info-overlay');
    const closeBtn = overlay?.querySelector('.pt-info-close');
    if (!btn || !overlay) return;

    document.body.appendChild(overlay);

    const close = () => {
        overlay.hidden = true;
        btn.setAttribute('aria-expanded', 'false');
    };

    btn.addEventListener('click', event => {
        event.stopPropagation();
        overlay.hidden = !overlay.hidden;
        btn.setAttribute('aria-expanded', String(!overlay.hidden));
    });

    closeBtn?.addEventListener('click', event => {
        event.stopPropagation();
        close();
    });

    overlay.addEventListener('click', event => {
        if (event.target !== overlay) return;
        close();
    });

    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') close();
    });
}
