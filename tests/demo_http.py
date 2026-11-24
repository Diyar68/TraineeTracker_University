"""Black-box demo checks. Run against a disposable Development demo instance."""
import http.cookiejar
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

base = sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:5189"
proxy_test = "--proxy" in sys.argv

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args):
        return None

def session():
    return urllib.request.build_opener(
        urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()), NoRedirect())

def request(client, path, data=None, method=None):
    body = urllib.parse.urlencode(data).encode() if data is not None else None
    try:
        response = client.open(urllib.request.Request(base + path, body, method=method))
    except urllib.error.HTTPError as error:
        response = error
    if proxy_test:
        for header in response.headers.get_all("Set-Cookie", []):
            if "traineetracker.auth=" in header.lower():
                check("secure" in header.lower() and "httponly" in header.lower(), "production auth cookie")
        # Emulate a browser's HTTPS connection to the TLS edge over local HTTP.
        for handler in client.handlers:
            if isinstance(handler, urllib.request.HTTPCookieProcessor):
                for cookie in handler.cookiejar:
                    cookie.secure = False
    return response.code, response.read().decode(), response.headers

def check(condition, message):
    if not condition:
        raise AssertionError(message)
    print("PASS", message)

client = session()
check(request(client, "/healthz")[0] == 200, "health")
check(request(client, "/TraineeDashboard")[0] == 302, "anonymous dashboard denied")
for role in ("trainee", "mentor"):
    client = session()
    code, html, _ = request(client, "/Account/Login")
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)[1]
    check(request(client, "/Account/DemoLogin", {"role": role})[0] == 400, "login requires antiforgery")
    check(request(client, "/Account/DemoLogin", {"role": "admin", "__RequestVerificationToken": token})[0] == 400, "admin login rejected")
    check(request(client, "/Account/DemoLogin", {"role": role, "__RequestVerificationToken": token})[0] == 302, role + " login")
    code, html, _ = request(client, "/TraineeDashboard")
    check(code == 200, role + " dashboard")
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)[1]
    for path in ("/Admin", "/CurriculumImport", "/Teachingplan/SaveOrder",
                 "/Progress/Transition", "/Feedback/CreateAjax", "/MentorFeedback/DeleteAjax"):
        check(request(client, path, {"__RequestVerificationToken": token})[0] == 403, role + " write denied " + path)
    check(request(client, "/Mentor/SelectTrainee?traineeId=demo-trainee")[0] in (404, 405), "GET selection rejected")
    code, _, _ = request(client, "/Mentor/SelectTrainee", {"traineeId": "unknown", "__RequestVerificationToken": token})
    check(code in (302, 403), "foreign trainee rejected")
    if role == "mentor":
        for trainee in ("demo-trainee", "demo-trainee-2"):
            check(request(client, "/Mentor/SelectTrainee", {"traineeId": trainee, "__RequestVerificationToken": token})[0] == 302, "assigned selection")
            check(request(client, "/TraineeDashboard")[0] == 200, "selected dashboard")

# Login throttling runs before token validation.
codes = [request(session(), "/Account/DemoLogin", {"role": "trainee"})[0] for _ in range(11)]
check(429 in codes, "login rate limited")
