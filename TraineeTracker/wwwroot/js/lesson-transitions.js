
/* ═══════════════════════════════════════════════════════
   LEKTIONSÜBERGÄNGE
═══════════════════════════════════════════════════════ */

let openLessonId      = null;
let pendingTransition = null;
let currentModalContentHtml = '';

/* ═══════════════════════════════════════════════════════
   TRANSITIONEN — AUSLÖSEN
═══════════════════════════════════════════════════════ */
function triggerTransition(from, to) {
    document.getElementById('modal-error')?.classList.remove('visible');
    clearTimeout(errorTimer);

    if (from === 'accepted' && to === 'rated' && getCurrentRole() === 'trainee') {
        openFeedbackForAcceptedLesson(openLessonId);
        return;
    }

    if (to === 'rejected') {
        showRejectionForm(from, to);
        return;
    }

    sendTransition(from, to, null);
}

function showRejectionForm(from, to) {
    const form = document.getElementById('rejection-form');
    if (form) form.style.display = 'block';
    pendingTransition = { from, to };

    const grid = document.getElementById('modal-actions');
    if (!grid) return;

    grid.innerHTML = `
        <button id="rejection-confirm-btn" class="action-btn primary" onclick="confirmRejection()">
            <span class="action-icon">✕</span>
            <span class="action-label">Ablehnung bestätigen</span>
            <span class="action-desc">Ablehnungsgrund speichern</span>
        </button>
        <button class="action-btn secondary" onclick="cancelRejectionMode()">
            <span class="action-icon">↩</span>
            <span class="action-label">Zurück</span>
            <span class="action-desc">Aktionen wieder anzeigen</span>
        </button>
    `;
}

function confirmRejection() {
    const reason = document.getElementById('rejection-reason')?.value.trim() ?? '';

    // Client-seitige Pflichtfeld-Prüfung — spart einen Roundtrip
    if (!reason) {
        showError('Bitte einen Ablehnungsgrund eingeben.');
        return;
    }

    sendTransition(pendingTransition.from, pendingTransition.to, reason);
}

function cancelRejectionMode() {
    const modalError = document.getElementById('modal-error');

    pendingTransition = null;
    if (modalError) modalError.classList.remove('visible');
    clearTimeout(errorTimer);

    if (currentModalContentHtml) setModalContent(currentModalContentHtml);
}


/* ═══════════════════════════════════════════════════════
   TRANSITIONEN — AJAX-REQUEST
   Validierung, Zeitstempel und Persistenz im Backend
═══════════════════════════════════════════════════════ */
async function sendTransition(from, to, rejectionNote) {
    setModalLoading(true);

    try {
        const res = await fetch('/Progress/Transition', {
            method:  'POST',
            headers: {
                'Content-Type':             'application/json',
                'RequestVerificationToken': getAntiForgeryToken(),
                'X-Demo-Role':              getCurrentRole(),
            },
            body: JSON.stringify({
                lectionId:     openLessonId,
                to:            to,
                rejectionNote: rejectionNote,
            }),
        });

        if (!res.ok) {
            const err = await readErrorResponse(res);
            if (currentModalContentHtml) setModalContent(currentModalContentHtml);
            showError(err.error ?? 'Fehler beim Statuswechsel.');
            return;
        }

        const data = await res.json();
        applyTransitionToUi(data);

    } catch {
        if (currentModalContentHtml) setModalContent(currentModalContentHtml);
        showError('Verbindungsfehler. Bitte erneut versuchen.');
    } finally {
        setModalLoading(false);
    }
}

async function readErrorResponse(res) {
    try {
        return await res.json();
    } catch {
        return { error: `Fehler beim Statuswechsel (${res.status}).` };
    }
}


/* ═══════════════════════════════════════════════════════
   TRANSITIONEN — UI NACH ERFOLG
   Aktualisiert Modal und delegiert Karten-/Statistik-Updates.
═══════════════════════════════════════════════════════ */
function applyTransitionToUi(data) {
    resetRejectionUi();

    if (data.modalContentHtml) setModalContent(data.modalContentHtml);

    updateCardInList(data);
    updateDashboardStats(data.stats);

    if (document.getElementById('view-wochenplan')?.classList.contains('active')
        && typeof reloadWeeklyPlan === 'function') {
        reloadWeeklyPlan();
    }

    if ((data.status === 'rated' || data.previousStatus === 'rated')
        && typeof reloadFeedbackView === 'function') {
        reloadFeedbackView();
    }

    const icon = data.status === 'rejected' ? '✕'
               : data.status === 'accepted' ? '✔' : '✓';
    showToast(`${data.title} → ${data.statusLabel ?? data.status}`, icon);
}

function resetRejectionUi() {
    const rejectionForm = document.getElementById('rejection-form');
    const rejectionReason = document.getElementById('rejection-reason');
    if (rejectionForm) rejectionForm.style.display = 'none';
    if (rejectionReason) rejectionReason.value = '';
    document.getElementById('rejection-confirm-btn')?.remove();
}
