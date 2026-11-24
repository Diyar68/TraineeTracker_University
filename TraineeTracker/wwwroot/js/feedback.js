/* ═══════════════════════════════════════════════════════
   DASHBOARD-FEEDBACK
═══════════════════════════════════════════════════════ */

function initDashboardFeedback() {
    document.querySelectorAll('[data-feedback-create-form]').forEach(form => {
        form.addEventListener('submit', submitDashboardFeedback);
    });

    updateFeedbackRoleState();
}

async function reloadFeedbackView() {
    try {
        const res = await fetch('/TraineeDashboard/FeedbackPartial', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
        });
        if (!res.ok) return;

        const html = await res.text();
        const container = document.getElementById('view-feedback');
        if (!container) return;

        const wrapper = document.createElement('div');
        wrapper.innerHTML = html;
        container.innerHTML = wrapper.firstElementChild?.innerHTML ?? html;
        initDashboardFeedback();
    } catch {
    }
}

function updateFeedbackRoleState() {
    const traineePanel = document.getElementById('feedback-trainee-panel');
    const mentorPanel = document.getElementById('feedback-mentor-panel');
    const roleNote = document.getElementById('feedback-role-note');
    if (!traineePanel || !mentorPanel || !roleNote) return;

    const isTrainee = getCurrentRole() === 'trainee';
    const isMentor = isMentorOrAdmin();
    traineePanel.style.display = isTrainee ? '' : 'none';
    mentorPanel.style.display = isMentor ? '' : 'none';
    roleNote.style.display = (isTrainee || isMentor) ? 'none' : 'block';
}

function switchFeedbackTab(name, button) {
    document.querySelectorAll('.feedback-tab').forEach(tab => tab.classList.remove('active'));
    button?.classList.add('active');

    document.querySelectorAll('#feedback-trainee-panel .feedback-panel').forEach(panel => panel.classList.remove('active'));
    document.getElementById(`feedback-tab-${name}`)?.classList.add('active');
}

function openFeedbackForAcceptedLesson(lectionId) {
    if (!lectionId) return;

    closeModal();
    const feedbackNav = [...document.querySelectorAll('.nav-pill a')]
        .find(link => link.textContent.trim() === 'Feedback');
    switchView('feedback', feedbackNav);
    switchFeedbackTab('own', document.querySelector("[data-feedback-tab='own']"));
    updateFeedbackRoleState();

    const form = document.querySelector(`[data-feedback-create-form][data-lection-id="${lectionId}"]`);
    if (!form) {
        showToast('Für diese Lektion ist kein Feedbackformular verfügbar.', '!');
        return;
    }

    form.scrollIntoView({ block: 'center', behavior: 'smooth' });
    form.classList.add('feedback-card-focus');
    setTimeout(() => form.classList.remove('feedback-card-focus'), 1600);
}

/* ═══════════════════════════════════════════════════════
   ANLEGEN — persistiert echtes Feedback in der Datenbank
   und setzt den Lektions-Fortschritt auf "rated"
═══════════════════════════════════════════════════════ */
async function submitDashboardFeedback(event) {
    event.preventDefault();

    const form = event.currentTarget;
    const lectionId = parseInt(form.dataset.lectionId ?? '0', 10);
    if (!lectionId) return;

    const submitButton = form.querySelector('button[type="submit"]');
    if (submitButton) submitButton.disabled = true;

    try {
        const res = await fetch('/Feedback/CreateAjax', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken(),
                'X-Demo-Role': getCurrentRole(),
            },
            body: JSON.stringify({
                lectionId: lectionId,
                difficulty: form.querySelector('[name="difficulty"]')?.value || null,
                priorKnowledge: form.querySelector('[name="priorKnowledge"]')?.value || null,
                effort: form.querySelector('[name="effort"]')?.value ? parseFloat(form.querySelector('[name="effort"]').value) : null,
                comment: form.querySelector('[name="comment"]')?.value || null,
            }),
        });

        if (!res.ok) {
            const err = await readErrorResponse(res);
            showToast(err.error ?? 'Feedback konnte nicht gespeichert werden.', '!');
            return;
        }

        const data = await res.json();

        // Lektionskarte im Lehrplan-Tab und Statistik-Kennzahlen synchron halten.
        if (typeof applyTransitionToUi === 'function' && data.transition) {
            openLessonId = lectionId;
            applyTransitionToUi(data.transition);
        }

        adjustFeedbackSectionCount(form.closest('.feedback-section'), -1);
        addOwnFeedbackCard(data.feedback);
        adjustFeedbackSectionCount(document.getElementById('own-feedback-list')?.closest('.feedback-section'), 1);
        form.remove();
        showToast('Feedback gespeichert', '✓');
    } catch {
        showToast('Feedback konnte nicht gespeichert werden.', '!');
    } finally {
        if (submitButton) submitButton.disabled = false;
    }
}

function addOwnFeedbackCard(feedback) {
    const list = document.getElementById('own-feedback-list');
    if (!list || !feedback) return;

    const title = feedback.lessonTitle ?? 'Lektion';
    const difficulty = feedback.difficulty ?? '';
    const effort = feedback.actualEffortDays ?? '';
    const priorKnowledge = feedback.priorKnowledge ?? '';
    const comment = feedback.comment ?? '';
    const today = new Date().toLocaleDateString('de-DE');

    const card = document.createElement('article');
    card.className = 'feedback-card own-feedback-card';
    card.dataset.feedbackId = feedback.id;
    card.dataset.lectionId = feedback.lectionId;
    card.dataset.lessonTitle = title;
    card.innerHTML = `
        <div class="feedback-card-view">
            <div class="feedback-card-top">
                <div>
                    <div class="feedback-date">${escapeHtml(today)}</div>
                    <h4>${escapeHtml(title)}</h4>
                </div>
                <span class="feedback-status">Bewertet</span>
            </div>
            <div class="feedback-meta">
                <span>${escapeHtml(difficulty || 'Keine Schwierigkeit')}</span>
                <span>${escapeHtml(effort ? `${effort} PT` : 'Kein Aufwand')}</span>
                <span class="feedback-prior-knowledge"><strong>Vorkenntnisse:</strong> ${escapeHtml(priorKnowledge || 'Keine Angabe')}</span>
            </div>
            <p>${escapeHtml(comment || 'Keine Anmerkung.')}</p>
            <div class="feedback-actions">
                <button type="button" class="feedback-btn" onclick="editFeedbackCard(this)">Bearbeiten</button>
                <button type="button" class="feedback-btn danger" onclick="deleteFeedbackCard(this)">Löschen</button>
            </div>
        </div>
        <form class="feedback-card-edit" hidden novalidate>
            <div class="feedback-form-grid">
                <label>Schwierigkeit
                    <select name="difficulty">
                        <option value="">Keine Angabe</option>
                        <option value="leicht" ${difficulty === 'leicht' ? 'selected' : ''}>leicht</option>
                        <option value="mittel" ${difficulty === 'mittel' ? 'selected' : ''}>mittel</option>
                        <option value="schwer" ${difficulty === 'schwer' ? 'selected' : ''}>schwer</option>
                    </select>
                </label>
                <label>Aufwand
                    <input name="effort" type="number" min="0" step="0.5" value="${escapeHtml(effort)}" />
                </label>
            </div>
            <label>Vorkenntnisse
                <input name="priorKnowledge" type="text" value="${escapeHtml(priorKnowledge)}" />
            </label>
            <label>Anmerkung
                <textarea name="comment" rows="3">${escapeHtml(comment)}</textarea>
            </label>
            <div class="feedback-actions">
                <button type="button" class="feedback-btn primary" onclick="saveFeedbackCard(this)">Speichern</button>
                <button type="button" class="feedback-btn" onclick="cancelFeedbackEdit(this)">Abbrechen</button>
            </div>
        </form>`;

    list.prepend(card);
}

function editFeedbackCard(button) {
    const card = button.closest('.own-feedback-card, .mentor-feedback-card');
    card?.querySelector('.feedback-card-view')?.setAttribute('hidden', '');
    card?.querySelector('.feedback-card-edit')?.removeAttribute('hidden');
}

function cancelFeedbackEdit(button) {
    const card = button.closest('.own-feedback-card, .mentor-feedback-card');
    card?.querySelector('.feedback-card-edit')?.setAttribute('hidden', '');
    card?.querySelector('.feedback-card-view')?.removeAttribute('hidden');
}

/* ═══════════════════════════════════════════════════════
   ÄNDERN — persistiert die Bearbeitung über das passende
   Ajax-Endpoint (eigenes Feedback vs. Mentor-Ansicht)
═══════════════════════════════════════════════════════ */
async function saveFeedbackCard(button) {
    const card = button.closest('.own-feedback-card, .mentor-feedback-card');
    if (!card) return;

    const feedbackId = card.dataset.feedbackId;
    if (!feedbackId) return;

    const isMentorCard = card.classList.contains('mentor-feedback-card');
    const endpoint = isMentorCard
        ? `/MentorFeedback/UpdateAjax?id=${encodeURIComponent(feedbackId)}`
        : `/Feedback/UpdateAjax?id=${encodeURIComponent(feedbackId)}`;

    const form = card.querySelector('.feedback-card-edit');
    const view = card.querySelector('.feedback-card-view');
    const difficulty = form?.querySelector('[name="difficulty"]')?.value || null;
    const effort = form?.querySelector('[name="effort"]')?.value ? parseFloat(form.querySelector('[name="effort"]').value) : null;
    const priorKnowledge = form?.querySelector('[name="priorKnowledge"]')?.value || null;
    const comment = form?.querySelector('[name="comment"]')?.value || null;

    button.disabled = true;

    try {
        const res = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken(),
                'X-Demo-Role': getCurrentRole(),
            },
            body: JSON.stringify({ difficulty, effort, priorKnowledge, comment }),
        });

        if (!res.ok) {
            const err = await readErrorResponse(res);
            showToast(err.error ?? 'Feedback konnte nicht aktualisiert werden.', '!');
            return;
        }

        const data = await res.json();
        const saved = data.feedback ?? {};

        const meta = view?.querySelectorAll('.feedback-meta span');
        if (meta?.[0]) meta[0].textContent = saved.difficulty || 'Keine Schwierigkeit';
        if (meta?.[1]) meta[1].textContent = saved.actualEffortDays ? `${saved.actualEffortDays} PT` : 'Kein Aufwand';
        if (meta?.[2]) meta[2].innerHTML = `<strong>Vorkenntnisse:</strong> ${escapeHtml(saved.priorKnowledge || 'Keine Angabe')}`;

        const text = view?.querySelector('p');
        if (text) text.textContent = saved.comment || 'Keine Anmerkung.';

        cancelFeedbackEdit(button);
        showToast('Feedback aktualisiert', '✓');
    } catch {
        showToast('Feedback konnte nicht aktualisiert werden.', '!');
    } finally {
        button.disabled = false;
    }
}

function adjustFeedbackSectionCount(section, delta) {
    const countEl = section?.querySelector('.feedback-count');
    if (!countEl) return;

    const match = countEl.textContent.match(/\d+/);
    if (!match) return;

    const next = Math.max(0, parseInt(match[0], 10) + delta);
    countEl.textContent = countEl.textContent.replace(/\d+/, String(next));
}

/* ═══════════════════════════════════════════════════════
   LÖSCHEN — persistiert das Löschen; bei eigenem Feedback
   springt die Lektion serverseitig zurück auf "accepted"
   und braucht wieder ein Formular zum erneuten Bewerten
═══════════════════════════════════════════════════════ */
async function deleteFeedbackCard(button) {
    const card = button.closest('.own-feedback-card, .mentor-feedback-card');
    if (!card) return;

    const feedbackId = card.dataset.feedbackId;
    if (!feedbackId) return;

    const isMentorCard = card.classList.contains('mentor-feedback-card');
    const endpoint = isMentorCard
        ? `/MentorFeedback/DeleteAjax?id=${encodeURIComponent(feedbackId)}`
        : `/Feedback/DeleteAjax?id=${encodeURIComponent(feedbackId)}`;

    button.disabled = true;

    try {
        const res = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getAntiForgeryToken(),
                'X-Demo-Role': getCurrentRole(),
            },
        });

        if (!res.ok) {
            const err = await readErrorResponse(res);
            showToast(err.error ?? 'Feedback konnte nicht gelöscht werden.', '!');
            return;
        }

        const data = await res.json();

        adjustFeedbackSectionCount(card.closest('.feedback-section'), -1);
        card.remove();

        if (!isMentorCard && data.deletion) {
            if (typeof applyTransitionToUi === 'function' && data.transition) {
                openLessonId = data.deletion.lectionId;
                applyTransitionToUi(data.transition);
            }
            restoreFeedbackCreateForm(data.deletion);
        }

        showToast('Feedback gelöscht', '✓');
    } catch {
        showToast('Feedback konnte nicht gelöscht werden.', '!');
    } finally {
        button.disabled = false;
    }
}

function restoreFeedbackCreateForm(deletion) {
    // "Feedback abgeben"-Sektion ist die erste .feedback-section im eigenen Tab.
    const createSection = document.querySelector('#feedback-tab-own .feedback-section');
    const grid = createSection?.querySelector('.feedback-grid-list');
    if (!grid) return;

    if (grid.querySelector(`[data-feedback-create-form][data-lection-id="${deletion.lectionId}"]`)) return;

    const form = document.createElement('form');
    form.className = 'feedback-card feedback-form-card';
    form.setAttribute('data-feedback-create-form', '');
    form.dataset.lectionId = deletion.lectionId;
    form.dataset.lectionTitle = deletion.lessonTitle;
    form.setAttribute('novalidate', '');
    form.innerHTML = `
        <div class="feedback-card-top">
            <div>
                <h4>${escapeHtml(deletion.lessonTitle ?? '')}</h4>
            </div>
            <span class="feedback-status">Akzeptiert</span>
        </div>
        <div class="feedback-form-grid">
            <label>
                Schwierigkeit
                <select name="difficulty">
                    <option value="">Keine Angabe</option>
                    <option value="leicht">leicht</option>
                    <option value="mittel">mittel</option>
                    <option value="schwer">schwer</option>
                </select>
            </label>
            <label>
                Aufwand
                <input name="effort" type="number" min="0" step="0.5" placeholder="PT" />
            </label>
        </div>
        <label>
            Vorkenntnisse
            <input name="priorKnowledge" type="text" placeholder="optional" />
        </label>
        <label>
            Anmerkung
            <textarea name="comment" rows="3" placeholder="optional"></textarea>
        </label>
        <div class="feedback-actions">
            <button type="submit" class="feedback-btn primary">Feedback speichern</button>
        </div>`;

    form.addEventListener('submit', submitDashboardFeedback);
    grid.prepend(form);
    adjustFeedbackSectionCount(createSection, 1);

    createSection?.querySelector('.feedback-empty')?.remove();
}
