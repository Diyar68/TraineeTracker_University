

const MENTOR_TRAINEE_KEY = 'traineeTracker.mentorTrainee';
const MENTOR_TRAINEE_STORAGE_KEY = MENTOR_TRAINEE_KEY;
const MENTOR_TRAINEE_COOKIE_NAME = MENTOR_TRAINEE_KEY;

/* STORAGE */

function getSelectedMentorTrainee() {
    try {
        return JSON.parse(localStorage.getItem(MENTOR_TRAINEE_STORAGE_KEY) || 'null');
    } catch {
        return null;
    }
}

function hasMentorTraineeSelection() {
    const selected = getSelectedMentorTrainee();
    return Boolean(selected?.id);
}

/* STATE */

function initMentorDashboard() {
    updateMentorDashboardState();
    initMentorSidebarScroll();
}



function initMentorSidebarScroll() {
    const sidebar = document.getElementById('mentor-sidebar') || document.querySelector('.mentor-side-nav');

    if (!sidebar) return;

    const startOffset = 24;

    const updateSidebar = () => {
        const scrollY = window.scrollY;
        sidebar.style.position = 'sticky';
        sidebar.style.top = `${startOffset}px`;
        sidebar.style.transform = `translateY(${scrollY > 0 ? scrollY : 0}px)`;
    };

    window.addEventListener('scroll', updateSidebar, { passive: true });
    window.addEventListener('resize', updateSidebar);
    updateSidebar();
}

function updateMentorDashboardState() {
    const isMentor = isMentorOrAdmin();
    const selected = getSelectedMentorTrainee();
    const hasSelection = Boolean(selected?.id);

    document.body.classList.toggle('mentor-role', isMentor);
    document.body.classList.toggle('mentor-needs-selection', isMentor && !hasSelection);
    document.body.classList.toggle('mentor-has-selection', isMentor && hasSelection);

    updateMentorFocusBanner(selected);
    updateMentorNavigation(isMentor, hasSelection);

    if (!isMentor) return;

    if (!hasSelection) {
        switchView('mentor', document.querySelector("[data-view-link='mentor']"));
    } else if (!document.querySelector('.view.active')
        || document.getElementById('view-mentor')?.classList.contains('active')) {
        switchView('lehrplan', document.querySelector("[data-view-link='lehrplan']"));
    }
}

/* NAVIGATION */

function openMentorTrainee(id) {
    const card = document.querySelector(`[data-mentor-trainee-card][data-trainee-id="${id}"]`);
    if (!card) return;

    // Speichere lokal für sofortige UI-Updates
    localStorage.setItem(MENTOR_TRAINEE_STORAGE_KEY, JSON.stringify({
        id,
        name: card.dataset.traineeName ?? 'Trainee',
        initials: card.dataset.traineeInitials ?? '',
        apiEmail: card.dataset.traineeApiEmail ?? '',
    }));

    // Setze das Server-seitige Cookie über die Controller-Action
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Mentor/SelectTrainee';
    for (const [name, value] of Object.entries({
        traineeId: id,
        __RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
    })) {
        const input = document.createElement('input');
        input.type = 'hidden'; input.name = name; input.value = value;
        form.appendChild(input);
    }
    document.body.appendChild(form);
    form.submit();
}

function backToMentorOverview() {
    clearMentorTraineeCookie();
    localStorage.removeItem(MENTOR_TRAINEE_STORAGE_KEY);
    updateMentorDashboardState();
}

function showMentorOverview() {
    clearMentorTraineeCookie();
    localStorage.removeItem(MENTOR_TRAINEE_STORAGE_KEY);
    updateMentorDashboardState();
    switchView('mentor', document.querySelector("[data-view-link='mentor']"));
}

function clearMentorTraineeCookie() {
    document.cookie = `${MENTOR_TRAINEE_COOKIE_NAME}=; path=/; max-age=0; SameSite=Lax`;
}

// Funktionen für die Naviagtion auf der Mentor Seite 


function scrollToTrainees() {
    document.getElementById('trainees-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function scrollToCurriculum() {
    document.getElementById('curriculum-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function scrollToFeedback() {
    document.getElementById('feedback-section')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

/* UI-UPDATES */

function updateMentorFocusBanner(selected) {
    const name = selected?.name ?? '';
    document.querySelectorAll('[data-selected-mentor-trainee]').forEach(el => {
        el.textContent = name;
    });

    document.querySelectorAll('[data-dashboard-trainee-full-name]').forEach(el => {
        if (name) el.textContent = name;
    });

    document.querySelectorAll('[data-dashboard-trainee-name]').forEach(el => {
        if (name) el.textContent = name;
    });

    document.querySelectorAll('[data-dashboard-trainee-initials]').forEach(el => {
        if (selected?.initials) el.textContent = selected.initials;
    });
}

function updateMentorNavigation(isMentor, hasSelection) {
    document.querySelectorAll('[data-requires-mentor-selection="true"]').forEach(link => {
        const blocked = isMentor && !hasSelection;
        link.classList.toggle('disabled', blocked);
        link.style.display = blocked ? 'none' : '';
        link.setAttribute('aria-disabled', blocked ? 'true' : 'false');
    });
}
