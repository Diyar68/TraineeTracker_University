// Run against a separate local Development demo and a headless Chrome debugging endpoint.
import fs from 'node:fs/promises';

const base = process.env.BROWSER_TEST_URL ?? 'http://127.0.0.1:5194';
const pages = await (await fetch('http://127.0.0.1:9517/json/list')).json();
const socket = new WebSocket(pages.find(page => page.type === 'page').webSocketDebuggerUrl);
await new Promise(resolve => socket.addEventListener('open', resolve, { once: true }));
const pending = new Map();
const errors = [];
let nextId = 1;
socket.addEventListener('message', event => {
    const result = JSON.parse(event.data);
    if (result.method === 'Runtime.exceptionThrown') errors.push(result.params.exceptionDetails.text);
    if (!pending.has(result.id)) return;
    const { resolve, reject } = pending.get(result.id);
    pending.delete(result.id);
    if (result.error) reject(new Error(JSON.stringify(result.error)));
    else resolve(result.result);
});
function command(method, params = {}) {
    const id = nextId++;
    return new Promise((resolve, reject) => {
        pending.set(id, { resolve, reject });
        socket.send(JSON.stringify({ id, method, params }));
    });
}
async function js(expression) {
    const response = await command('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (response.exceptionDetails) throw new Error(JSON.stringify(response.exceptionDetails));
    return response.result.value;
}
async function waitFor(expression) {
    for (let attempt = 0; attempt < 100; attempt++) {
        if (await js(expression)) return;
        await new Promise(resolve => setTimeout(resolve, 100));
    }
    throw new Error('Timed out waiting for ' + expression);
}
async function login(role) {
    await command('Page.navigate', { url: base + '/Account/Login' });
    await waitFor("document.querySelector('[name=Email]') !== null");
    const password = { trainee: 'TraineeDemo!2026', mentor: 'MentorDemo!2026', admin: 'AdminDemo!2026' }[role];
    await js(`document.querySelector('[name=Email]').value=${JSON.stringify(role + '@demo.example')};
        document.querySelector('[name=Password]').value=${JSON.stringify(password)};
        document.querySelector('form.login-form').requestSubmit();`);
    await waitFor("!location.pathname.startsWith('/Account') && document.readyState === 'complete'");
}
async function logout() {
    await js("document.querySelector('form[action*=Logout]').requestSubmit()");
    await waitFor("location.pathname.startsWith('/Account') && document.querySelector('[name=Email]') !== null");
}
async function screenshot(name) {
    await js('window.scrollTo(0, 0)');
    await new Promise(resolve => setTimeout(resolve, 300));
    const result = await command('Page.captureScreenshot', { format: 'png' });
    await fs.mkdir('docs/screenshots', { recursive: true });
    await fs.writeFile('docs/screenshots/' + name + '.png', Buffer.from(result.data, 'base64'));
}
try {
    await command('Network.clearBrowserCookies');
    await command('Runtime.enable');
    await command('Page.enable');
    await command('Emulation.setDeviceMetricsOverride', {
        width: 1440, height: 1100, deviceScaleFactor: 1, mobile: false
    });
    await login('trainee');
    await waitFor("document.querySelectorAll('.lesson-card').length === 62");
    await screenshot('trainee-dashboard');
    await js("switchView('statistik', document.querySelector('[data-view-link=statistik]'))");
    await screenshot('progress-statistics');
    await js("switchView('wochenplan', document.querySelector('[data-view-link=wochenplan]'))");
    await screenshot('weekly-plan');
    await logout();
    await login('mentor');
    await screenshot('mentor-overview');
    await logout();
    await login('admin');
    await waitFor("document.body.innerText.includes('trainee@demo.example')");
    await screenshot('user-administration');
    if (errors.length) throw new Error('Browser exceptions: ' + errors.join(', '));
    console.log('PASS three browser logins, 62 lesson cards, statistics, weekly plan, mentor and admin UI');
} finally {
    socket.close();
}
