"""Functional regression checks against a disposable isolated demo instance."""
import copy
import concurrent.futures
import http.cookiejar
import json
import pathlib
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:5193"
PROXY = "--proxy" in sys.argv

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args):
        return None

def check(ok, message):
    if not ok:
        raise AssertionError(message)
    print("PASS", message)

class Browser:
    def __init__(self):
        self.jar = http.cookiejar.CookieJar()
        self.client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar), NoRedirect())
        self.token = ""

    def request(self, path, data=None, json_body=None, upload=None):
        headers = {}
        body = None
        if data is not None:
            body = urllib.parse.urlencode({**data, "__RequestVerificationToken": self.token}, doseq=True).encode()
        if json_body is not None:
            body = json.dumps(json_body).encode()
            headers = {"Content-Type": "application/json", "RequestVerificationToken": self.token}
        if upload is not None:
            boundary = "portfolio-test-boundary"
            body = (f'--{boundary}\r\nContent-Disposition: form-data; name="__RequestVerificationToken"\r\n\r\n{self.token}\r\n'
                    f'--{boundary}\r\nContent-Disposition: form-data; name="curriculumFile"; filename="curriculum.json"\r\n'
                    f'Content-Type: application/json\r\n\r\n{json.dumps(upload)}\r\n--{boundary}--\r\n').encode()
            headers["Content-Type"] = "multipart/form-data; boundary=" + boundary
        try:
            response = self.client.open(urllib.request.Request(BASE + path, body, headers), timeout=30)
        except urllib.error.HTTPError as error:
            response = error
        text = response.read().decode()
        tokens = re.findall(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', text)
        if tokens:
            self.token = tokens[0]
        if PROXY:
            for cookie in self.jar:
                cookie.secure = False  # Emulate the browser-to-Render HTTPS hop over local HTTP.
        return response.code, text, response.headers

    def login(self, role):
        self.request("/Account/Login")
        password = {"trainee": "TraineeDemo!2026", "mentor": "MentorDemo!2026", "admin": "AdminDemo!2026"}[role]
        code, _, headers = self.request("/Account/Login", {"Email": role + "@demo.example", "Password": password})
        check(code == 302, role + " password login")
        if PROXY:
            cookie = next(h for h in headers.get_all("Set-Cookie") if h.startswith("TraineeTracker.Auth="))
            check("secure" in cookie.lower() and "httponly" in cookie.lower(), role + " secure auth cookie")
        check(self.request(headers["Location"])[0] == 200, role + " landing page")
        health_code, _, health_headers = self.request("/healthz")
        check(health_code == 200 and not health_headers.get_all("Set-Cookie"), "authenticated stateless health")

    def transition(self, lesson, target, expected=200):
        code, text, _ = self.request("/Progress/Transition", json_body={"lectionId": lesson, "to": target})
        check(code == expected, "transition " + target)
        return text

def run_checks():
    check(Browser().request("/healthz")[0] == 200, "public health")
    anonymous = Browser()
    check(anonymous.request("/TraineeDashboard")[0] == 302, "anonymous access denied")
    # Missing CSRF token must be rejected even with valid public credentials.
    code, _, _ = anonymous.request("/Account/Login", {"Email": "admin@demo.example", "Password": "AdminDemo!2026"})
    check(code == 302, "invalid login CSRF submission rejected with fresh-form redirect")
    check(not any(cookie.name == "TraineeTracker.Auth" for cookie in anonymous.jar), "invalid CSRF does not authenticate")
    check(anonymous.request("/TraineeDashboard")[0] == 302, "invalid CSRF cannot access dashboard")
    
    trainee = Browser()
    trainee.login("trainee")
    for path in ("/TraineeDashboard/WeeklyPlanPartial", "/TraineeDashboard/FeedbackPartial", "/TraineeDashboard/LectionDetail/1"):
        check(trainee.request(path)[0] == 200, "trainee view " + path)
    check(trainee.request("/Admin")[0] == 302, "trainee admin access denied")
    check(trainee.request("/DemoLessons/509332")[0] == 200, "local fictional lesson content")
    trainee.transition(1, "Started")
    trainee.transition(1, "Accepted", 403)
    trainee.transition(1, "Finished")
    check(trainee.request("/Teachingplan/SaveOrder", json_body=[{"lessonId": 1, "individualOrderIndex": 7}])[0] == 200, "save lesson order")
    check(trainee.request("/Mentor/SelectTrainee", {"traineeId": "seed-trainee-2"})[0] == 302, "trainee mentor action denied")
    
    mentor = Browser()
    mentor.login("mentor")
    check(mentor.request("/Admin")[0] == 302, "mentor admin access denied")
    check(mentor.request("/Mentor/SelectTrainee?traineeId=dev-trainee-01")[0] in (404, 405), "selection GET denied")
    check(mentor.request("/Mentor/SelectTrainee", {"traineeId": "unknown"})[0] == 403, "unassigned selection denied")
    for trainee_id in ("dev-trainee-01", "seed-trainee-2", "seed-trainee-3"):
        check(mentor.request("/Mentor/SelectTrainee", {"traineeId": trainee_id})[0] == 302, "assigned trainee selection")
        check(mentor.request("/TraineeDashboard")[0] == 200, "mentor trainee dashboard")
    mentor.request("/Mentor/SelectTrainee", {"traineeId": "dev-trainee-01"})
    mentor.transition(1, "Accepted")
    # One browser may switch roles in its own sandbox to complete the review/feedback workflow.
    mentor.request("/Account/Logout", {})
    mentor.login("trainee")
    code, text, _ = mentor.request("/Feedback/CreateAjax", json_body={"lectionId": 1, "difficulty": "mittel", "effort": 2, "comment": "Synthetic feedback"})
    check(code == 200, "feedback create")
    feedback_id = json.loads(text)["feedback"]["id"]
    for comment in ("Synthetic update",):
        check(mentor.request("/Feedback/UpdateAjax/" + str(feedback_id), json_body={"difficulty": "mittel", "effort": 3, "comment": comment})[0] == 200, "feedback edit")
    check(mentor.request("/Feedback/DeleteAjax/" + str(feedback_id), json_body={})[0] == 200, "feedback delete")
    mentor.request("/Account/Logout", {})
    mentor.request("/Account/Login")
    check(mentor.request("/Account/Login", {"Email": "casey@demo.example", "Password": "CaseyDemo!2026"})[0] == 302, "second mentor login")
    mentor.request("/TraineeDashboard")
    check(mentor.request("/Mentor/SelectTrainee", {"traineeId": "dev-trainee-01"})[0] == 403, "known unassigned trainee denied")
    check(mentor.request("/Mentor/SelectTrainee", {"traineeId": "seed-trainee-3"})[0] == 302, "second mentor assigned trainee allowed")
    
    admin = Browser()
    admin.login("admin")
    other = Browser()
    other.login("admin")
    # Concurrent sessions must remain independent.
    with concurrent.futures.ThreadPoolExecutor() as pool:
        codes = list(pool.map(lambda b: b.request("/Admin")[0], [admin, other]))
    check(codes == [200, 200], "parallel independent sessions")
    form = {"FirstName": "Isolated", "LastName": "Example", "Email": "isolated@demo.example",
            "Password": "Synthetic!2026", "Role": "Trainee", "TraineeStartDate": "2026-01-01",
            "TraineeEndDate": "2027-12-31", "AssignedMentorIds": "seed-mentor-1"}
    check(admin.request("/Admin/Create", {**form, "Role": "InfrastructureAdmin"})[0] == 200, "invalid role rejected")
    check("isolated@demo.example" not in admin.request("/Admin")[1], "invalid user was not created")
    check(admin.request("/Admin/Create", form)[0] == 302, "admin create user")
    code, html, _ = admin.request("/Admin")
    check("isolated@demo.example" in html, "created user visible")
    match = re.search(r'href="/Admin/Edit/([^"]+)"[^>]*>[^<]*Bearbeiten', html)
    # Match the edit link in the created user's card.
    position = html.index("isolated@demo.example")
    user_id = re.search(r'/Admin/Edit/([^"?#]+)', html[position:])[1]
    check("isolated@demo.example" not in other.request("/Admin")[1], "visitor data isolation")
    admin.request("/Admin/Edit/" + user_id)
    check(admin.request("/Admin/Edit/" + user_id, {**form, "Id": user_id, "FirstName": "Updated", "Password": ""})[0] == 302, "admin edit user")
    check(admin.request("/Admin/Close/" + user_id, {})[0] == 302, "admin close user")
    check(admin.request("/Admin/Close/" + user_id, {})[0] == 302, "admin reopen user")
    admin.request("/CurriculumImport")
    curriculum = json.loads((pathlib.Path(__file__).parents[1] / "TraineeTracker/Data/Seed/lehrplan_devop.json").read_text())
    curriculum[0]["title"] = "Isolated curriculum update"
    curriculum.append({"id": 999999, "title": "Isolated new lesson", "url": "https://curriculum.example/new", "estimate": 1, "deprecated": False})
    code, html, _ = admin.request("/CurriculumImport", upload=curriculum)
    check(code == 200 and "Isolated new lesson" in html and "Isolated curriculum update" in html, "import creates and updates lessons")
    check("Isolated new lesson" not in other.request("/CurriculumImport")[1], "import isolation")
    # Original import deprecates omitted lessons instead of hard-deleting progressed lessons.
    check(admin.request("/CurriculumImport", upload=curriculum[:-1])[0] == 200, "import removes omitted lesson from active curriculum")
    check(admin.request("/Account/ResetDemo", {})[0] == 302, "reset sandbox")
    admin.login("admin")
    check("isolated@demo.example" not in admin.request("/Admin")[1], "reset restores users")
    check("Isolated curriculum update" not in admin.request("/CurriculumImport")[1], "reset restores curriculum")
    # Forged role display cookie cannot confer admin access.
    trainee.client.addheaders = [("Cookie", "role=admin")]
    check(trainee.request("/Admin")[0] == 302, "forged role cookie denied")
    for cookie in admin.jar:
        if cookie.name == "TraineeTracker.Auth":
            other.jar.set_cookie(copy.copy(cookie))
    check(other.request("/Admin")[0] == 302, "auth cookie bound to sandbox")
    print("All functional HTTP checks passed")

if __name__ == "__main__":
    run_checks()
