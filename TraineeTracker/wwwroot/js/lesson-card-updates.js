
/* ═══════════════════════════════════════════════════════
   LEKTIONSKARTEN — DOM-UPDATES
═══════════════════════════════════════════════════════ */

function updateCardInList(data) {
    const card = document.querySelector(`.lesson-card[data-id="${openLessonId}"]`);
    if (!card) return;

    const updatedCard = createCardFromHtml(data.cardHtml);
    if (!updatedCard) return;

    card.replaceWith(updatedCard);
    moveCardToGroup(updatedCard);
}

function createCardFromHtml(cardHtml) {
    if (!cardHtml) return null;

    const template = document.createElement('template');
    template.innerHTML = cardHtml.trim();
    const card = template.content.firstElementChild;

    return card?.classList?.contains('lesson-card') ? card : null;
}

function moveCardToGroup(card) {
    const targetLabel = card.dataset.groupLabel;
    const container   = document.getElementById('lessons-container');
    if (!container || !targetLabel) return;

    const groups      = [...container.querySelectorAll('.lesson-group')];
    let   targetGroup = groups.find(g =>
        g.querySelector('.group-title')?.textContent === targetLabel
    );

    if (!targetGroup) {
        targetGroup           = document.createElement('div');
        targetGroup.className = 'lesson-group reveal';
        targetGroup.innerHTML = `<div class="group-title">${targetLabel}</div>
                                 <div class="lessons-list"></div>`;
        container.appendChild(targetGroup);
    }

    targetGroup.querySelector('.lessons-list')?.appendChild(card);

    groups.forEach(g => {
        if (!g.querySelector('.lesson-card')) g.remove();
    });

    applySortToDom();
    applyFilters();
}
