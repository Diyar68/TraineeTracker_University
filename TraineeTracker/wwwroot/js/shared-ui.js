/* ═══════════════════════════════════════════════════════
   SHARED UI — allgemeine Helfer
═══════════════════════════════════════════════════════ */

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function closeModal() {
    document.getElementById('modal-overlay')?.classList.remove('open');

    openLessonId      = null;
    pendingTransition = null;

    const rejectionForm   = document.getElementById('rejection-form');
    const rejectionReason = document.getElementById('rejection-reason');
    const modalError      = document.getElementById('modal-error');
    if (rejectionForm)   rejectionForm.style.display = 'none';
    if (rejectionReason) rejectionReason.value       = '';
    if (modalError)      modalError.classList.remove('visible');
    document.getElementById('rejection-confirm-btn')?.remove();

    clearTimeout(errorTimer);
}

function handleOverlayClick(e) {
    if (e.target === document.getElementById('modal-overlay')) closeModal();
}

document.addEventListener('keydown', e => {
    if (e.key === 'Escape') closeModal();
});

function showError(msg) {
    const el = document.getElementById('modal-error');
    if (!el) return;
    el.textContent = msg;
    el.classList.add('visible');
    clearTimeout(errorTimer);
    errorTimer = setTimeout(() => el.classList.remove('visible'), 3500);
}

function showToast(msg, icon = '✓') {
    const toast    = document.getElementById('toast');
    const toastMsg = document.getElementById('toast-msg');
    const toastIco = document.getElementById('toast-icon');
    if (!toast || !toastMsg || !toastIco) return;

    toastMsg.textContent = msg;
    toastIco.textContent = icon;

    clearTimeout(toastTimer);
    toast.classList.add('show');
    toastTimer = setTimeout(() => toast.classList.remove('show'), 2800);
}

function escapeHtml(value) {
    return String(value)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}
