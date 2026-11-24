/* ═══════════════════════════════════════════════════════
   ROLLEN- UND VIEW-WECHSEL
   Temporäre Demo-Rollenquelle ist ausschließlich das role-Cookie.
═══════════════════════════════════════════════════════ */

const ROLE_COOKIE_NAME = 'role';
const ROLE_TRAINEE = 'trainee';
const ROLE_MENTOR = 'mentor';
const ROLE_ADMIN = 'admin';

function switchView(name, el) {
    // Mentoren (und Admins, die den Mentor-Bereich nutzen) brauchen zuerst
    // einen Trainee-Kontext; direkte Dashboard-Tabs werden bis dahin auf die
    // Übersicht umgeleitet.
    if (isMentorOrAdmin()
        && name !== 'mentor'
        && typeof hasMentorTraineeSelection === 'function'
        && !hasMentorTraineeSelection()) {
        name = 'mentor';
        el = document.querySelector("[data-view-link='mentor']");
    }

    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    document.getElementById('view-' + name)?.classList.add('active');
    document.querySelectorAll('.nav-pill a').forEach(a => a.classList.remove('active'));
    el?.classList.add('active');

    // Der Status-Filter im Sidebar bezieht sich nur auf die Lehrplan-Lektionsliste.
    const statusFilter = document.getElementById('sidebar-status-filter');
    if (statusFilter) statusFilter.style.display = name === 'lehrplan' ? '' : 'none';

    if (name === 'wochenplan') renderCalendar();
}

function initRoleToggle() {
    updateRoleNavigation();
    if (typeof updateMentorDashboardState === 'function') updateMentorDashboardState();
}

function getCurrentRole() {
    const roleMatch = document.cookie.match(new RegExp(`(?:^|;\\s*)${ROLE_COOKIE_NAME}=(${ROLE_TRAINEE}|${ROLE_MENTOR}|${ROLE_ADMIN})`));
    const role = roleMatch ? roleMatch[1] : ROLE_TRAINEE;
    return role === ROLE_MENTOR || role === ROLE_ADMIN ? role : ROLE_TRAINEE;
}

// Admins dürfen den Mentor-Bereich genauso nutzen wie Mentoren — für alle
// Dashboard-Ansichten (Navigation, Trainee-Auswahl, Feedback-Panel, ...)
// zählt ein Admin daher als Mentor.
function isMentorOrAdmin() {
    const role = getCurrentRole();
    return role === ROLE_MENTOR || role === ROLE_ADMIN;
}

function updateRoleNavigation() {
    document.querySelectorAll('.nav-pill a[data-visible-roles]').forEach(link => {
        const roles = (link.dataset.visibleRoles ?? '').split(' ');
        const role = getCurrentRole();
        // "mentor"-Elemente sind auch für Admins sichtbar; "admin"-Elemente bleiben Admins vorbehalten.
        const isVisible = roles.includes(role) || (role === ROLE_ADMIN && roles.includes(ROLE_MENTOR));
        link.style.display = isVisible ? '' : 'none';
        if (!isVisible && link.classList.contains('active')) {
            const fallback = isMentorOrAdmin()
                ? document.querySelector("[data-view-link='mentor']")
                : document.querySelector("[data-view-link='lehrplan']");
            switchView(fallback?.dataset.viewLink ?? 'lehrplan', fallback);
        }
    });
}
