/**
 * Codeownership: Diyar Altintas
 */
/* ═══════════════════════════════════════════════════════
   WOCHENPLAN / KALENDER
   Serverseitig gerenderte Partial; JS steuert nur Navigation.
═══════════════════════════════════════════════════════ */

let weeklyPlanStartDate = null;

function shiftWeek(delta) {
    const current = getWeeklyPlanStartDate();
    reloadWeeklyPlan(addDays(current, delta * 7));
}

function goToToday() {
    reloadWeeklyPlan(getMonday(new Date()));
}

async function renderCalendar() {
    await reloadWeeklyPlan(getWeeklyPlanStartDate());
}

async function reloadWeeklyPlan(fromDate = null) {
    const requestedDate = fromDate ?? getWeeklyPlanStartDate();
    const fromKey = dkey(getMonday(requestedDate));

    try {
        const res = await fetch(`/TraineeDashboard/WeeklyPlanPartial?from=${fromKey}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
        });

        if (!res.ok) throw new Error('Netzwerkfehler');

        const html = await res.text();
        const container = document.getElementById('weekly-plan-container');
        if (!container) return;

        container.innerHTML = html;
        syncWeeklyPlanStateFromDom();
    } catch {
        showToast('Kalenderdaten konnten nicht geladen werden.', '⚠');
    }
}

function syncWeeklyPlanStateFromDom() {
    const grid = document.getElementById('cal-grid');
    const weekStart = grid?.dataset.weekStart;
    if (weekStart) weeklyPlanStartDate = parseDateKey(weekStart);

    const weekLabel = grid?.dataset.weekLabel;
    const label = document.getElementById('cal-week-label');
    if (label && weekLabel) label.textContent = weekLabel;
}

function getWeeklyPlanStartDate() {
    if (weeklyPlanStartDate) return weeklyPlanStartDate;

    const existingStart = document.getElementById('cal-grid')?.dataset.weekStart;
    weeklyPlanStartDate = existingStart
        ? parseDateKey(existingStart)
        : getMonday(new Date());

    return weeklyPlanStartDate;
}

function parseDateKey(value) {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
}

function getMonday(date) {
    const result = new Date(date);
    result.setHours(0, 0, 0, 0);
    const day = result.getDay() || 7;
    result.setDate(result.getDate() - day + 1);
    return result;
}

function addDays(date, days) {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
}

function dkey(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
