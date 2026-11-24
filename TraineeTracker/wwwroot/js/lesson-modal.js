
/* ═══════════════════════════════════════════════════════
   LEKTIONSMODAL
═══════════════════════════════════════════════════════ */

async function openLesson(id) {
    openLessonId      = id;
    pendingTransition = null;

    const modalError      = document.getElementById('modal-error');
    if (modalError)      modalError.classList.remove('visible');
    clearTimeout(errorTimer);

    document.getElementById('modal-overlay')?.classList.add('open');
    setModalContent('');
    setModalLoading(true);

    try {
        const res  = await fetch(`/TraineeDashboard/LectionDetail/${id}?role=${encodeURIComponent(getCurrentRole())}`, {
            headers: { 'X-Demo-Role': getCurrentRole() },
        });
        if (!res.ok) throw new Error('Netzwerkfehler');
        const html = await res.text();
        setModalContent(html);
    } catch {
        showError('Lektion konnte nicht geladen werden.');
    } finally {
        setModalLoading(false);
    }
}

function setModalLoading(on) {
    if (!on) return;

    const grid = document.getElementById('modal-actions');
    if (grid) {
        grid.innerHTML = `<div style="grid-column:1/-1;text-align:center;color:var(--gray3);font-family:var(--ui);font-size:12px;padding:16px;">
            Lädt…
        </div>`;
        return;
    }

    const content = document.getElementById('modal-content');
    if (!content) return;
    content.innerHTML = `<div id="modal-actions" class="modal-actions">
        <div style="grid-column:1/-1;text-align:center;color:var(--gray3);font-family:var(--ui);font-size:12px;padding:16px;">
            Lädt…
        </div>
    </div>`;
}

function setModalContent(html) {
    const content = document.getElementById('modal-content');
    if (!content) return;
    content.innerHTML = html;
    currentModalContentHtml = html;
}
