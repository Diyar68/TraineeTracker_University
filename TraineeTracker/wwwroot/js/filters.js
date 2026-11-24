
/* ═══════════════════════════════════════════════════════
   FILTER, SUCHE UND SORTIERUNG
═══════════════════════════════════════════════════════ */

/* Zustand und Initialisierung */
let activeFilter = 'all';
let sortMode     = 'priority';   // 'priority' | 'order'
let hideInactive = false;

function initSearch() {
    const searchInput = document.getElementById('search');
    if (searchInput) {
        searchInput.addEventListener('input', applyFilters);
    }
}

/* Suche und Statusfilter */
function setFilter(el, status) {
    document.querySelectorAll('.filter-item').forEach(f => f.classList.remove('active'));
    el.classList.add('active');
    activeFilter = status;

    if (status === 'all') {
        setSortMode('priority');
        applySortToDom();
    } else {
        applyStatusNavigationLayout(el.dataset.label ?? status);
    }

    applyFilters();
}

function applyStatusNavigationLayout(label) {
    const container = document.getElementById('lessons-container');
    if (!container) return;

    const allCards = [...container.querySelectorAll('.lesson-card')];
    const group = ensureLessonGroup(label);
    const list = group.querySelector('.lessons-list');
    if (!list) return;

    container.insertBefore(group, document.getElementById('empty-state'));

    const titleEl = group.querySelector('.group-title');
    if (titleEl) titleEl.textContent = label;

    allCards
        .sort(compareCardsByCurriculumOrder)
        .forEach(card => list.appendChild(card));

    removeEmptyLessonGroups();
}

function applyFilters() {
    const query = (document.getElementById('search')?.value ?? '').toLowerCase().trim();

    document.querySelectorAll('.lesson-card').forEach(card => {
        const matchesFilter   = activeFilter === 'all' || card.dataset.status === activeFilter;
        const searchText      = card.dataset.search ?? '';
        const matchesSearch   = !query || searchText.includes(query);
        const matchesInactive = !hideInactive || card.dataset.status !== 'skipped';

        card.style.display = (matchesFilter && matchesSearch && matchesInactive) ? '' : 'none';
    });

    // Leere Gruppen ausblenden
    document.querySelectorAll('.lesson-group').forEach(group => {
        const hasVisible = [...group.querySelectorAll('.lesson-card')]
            .some(c => c.style.display !== 'none');
        group.style.display = hasVisible ? '' : 'none';
    });

    updateEmptyState();
}

/* Empty-State */
function updateEmptyState() {
    const anyVisible = [...document.querySelectorAll('.lesson-card')]
        .some(c => c.style.display !== 'none');
    const emptyState = document.getElementById('empty-state');
    if (emptyState) emptyState.style.display = anyVisible ? 'none' : '';
}

/* Sortiermodus */
function toggleSort() {
    sortMode = sortMode === 'priority' ? 'order' : 'priority';
    updateSortButton();

    applySortToDom();
    applyFilters();
}

function setSortMode(mode) {
    sortMode = mode === 'order' ? 'order' : 'priority';
    updateSortButton();
}

function updateSortButton() {
    const btn = document.getElementById('sort-btn');
    if (!btn) return;

    btn.textContent = sortMode === 'priority'
        ? 'Sortierung: Handlungsbedarf'
        : 'Sortierung: Lehrplanreihenfolge';
    btn.classList.toggle('active', sortMode === 'priority');
}

/* DOM-Sortierung und Gruppierung */
function applySortToDom() {
    const container = document.getElementById('lessons-container');
    if (!container) return;

    const allCards = [...container.querySelectorAll('.lesson-card')];

    if (sortMode === 'priority') {
        container.querySelectorAll('.group-title[data-original-title]').forEach(t => {
            t.textContent = t.dataset.originalTitle;
            delete t.dataset.originalTitle;
        });

        getGroupsFromCards(allCards).forEach(groupDef => {
            const group = ensureLessonGroup(groupDef.label);
            const list = group.querySelector('.lessons-list');
            if (!list) return;

            container.insertBefore(group, document.getElementById('empty-state'));

            allCards
                .filter(card => getCardGroupLabel(card) === groupDef.label)
                .sort((a, b) => compareCardsForGroup(a, b, groupDef))
                .forEach(card => list.appendChild(card));
        });

        removeEmptyLessonGroups();
    } else {
        // Flat-Ansicht: alle Karten in erste Gruppe, Rest ausblenden
        const allGroups = [...container.querySelectorAll('.lesson-group')];
        if (!allGroups.length) return;

        const targetGroup = allGroups[0];
        const targetList  = targetGroup.querySelector('.lessons-list');
        if (!targetList) return;

        // Original-Titel merken
        const titleEl = targetGroup.querySelector('.group-title');
        if (titleEl && !titleEl.dataset.originalTitle) {
            titleEl.dataset.originalTitle = titleEl.textContent;
        }
        if (titleEl) titleEl.textContent = 'Alle Lektionen';

        allGroups.slice(1).forEach(g => g.style.display = 'none');

        allCards
            .sort(compareCardsByCurriculumOrder)
            .forEach(card => targetList.appendChild(card));
    }
}

function compareCardsForGroup(a, b, groupDef) {
    if (groupDef.usesStatusPriority) {
        const priorityDiff = getCardStatusPriority(a) - getCardStatusPriority(b);
        if (priorityDiff !== 0) return priorityDiff;
    }

    return compareCardsByCurriculumOrder(a, b);
}

function getGroupsFromCards(cards) {
    const groups = new Map();

    cards.forEach(card => {
        const label = getCardGroupLabel(card);
        if (!groups.has(label)) {
            groups.set(label, {
                label,
                order: getCardGroupOrder(card),
                usesStatusPriority: card.dataset.groupUsesPriority === 'true',
            });
        }
    });

    return [...groups.values()]
        .sort((a, b) => a.order - b.order || a.label.localeCompare(b.label));
}

function getCardGroupLabel(card) {
    return card.dataset.groupLabel ?? 'Weitere';
}

function getCardGroupOrder(card) {
    return parseInt(card.dataset.groupOrder ?? '99', 10);
}

function getCardStatusPriority(card) {
    return parseInt(card.dataset.statusPriority ?? '99', 10);
}

function compareCardsByCurriculumOrder(a, b) {
    const orderDiff = getCardOrder(a) - getCardOrder(b);
    if (orderDiff !== 0) return orderDiff;

    return getCardTitle(a).localeCompare(getCardTitle(b));
}

function getCardOrder(card) {
    const order = parseInt(card.dataset.order ?? '', 10);
    return Number.isFinite(order) && order >= 0 ? order : Number.MAX_SAFE_INTEGER;
}

function getCardTitle(card) {
    return card.querySelector('.lesson-title')?.textContent?.trim() ?? '';
}

function ensureLessonGroup(label) {
    const container = document.getElementById('lessons-container');
    let group = [...container.querySelectorAll('.lesson-group')]
        .find(g => g.querySelector('.group-title')?.textContent === label);

    if (!group) {
        group = document.createElement('div');
        group.className = 'lesson-group reveal';
        group.innerHTML = `<div class="group-title">${label}</div>
                           <div class="lessons-list"></div>`;
        container.insertBefore(group, document.getElementById('empty-state'));
    }

    group.style.display = '';
    const titleEl = group.querySelector('.group-title');
    if (titleEl) titleEl.textContent = label;

    return group;
}

function removeEmptyLessonGroups() {
    document.querySelectorAll('.lesson-group').forEach(group => {
        if (!group.querySelector('.lesson-card')) group.remove();
    });
}

/* Inaktive Lektionen */
function toggleInactive() {
    hideInactive = !hideInactive;

    const btn = document.getElementById('inactive-btn');
    if (btn) {
        btn.textContent       = hideInactive ? 'Inaktive ausgeblendet' : 'Inaktive sichtbar';
        btn.style.borderColor = hideInactive ? 'var(--red)'   : 'var(--border)';
        btn.style.color       = hideInactive ? 'var(--white)' : '';
        btn.classList.toggle('active', hideInactive);
    }

    applySortToDom();
    applyFilters();
}
