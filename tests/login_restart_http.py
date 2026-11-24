"""Reproduce a stale login form across a restart of a disposable local Docker container."""
import subprocess
import sys
import time
from urllib.parse import urlparse

import functional_http as t

t.BASE = sys.argv[1]
t.PROXY = True
if urlparse(t.BASE).hostname not in ("localhost", "127.0.0.1"):
    raise SystemExit("Use a disposable local instance for this restart test.")
container = sys.argv[2]
browser = t.Browser()
assert browser.request("/Account/Login")[0] == 200
old_token = browser.token
authenticated = t.Browser()
authenticated.login("admin")
subprocess.run(["docker", "restart", container], check=True, capture_output=True)
for attempt in range(100):
    try:
        if t.Browser().request("/healthz")[0] == 200:
            break
    except Exception:
        pass
    time.sleep(0.2)
browser.token = old_token
code, _, headers = browser.request("/Account/Login",
    {"Email": "trainee@demo.example", "Password": "TraineeDemo!2026"})
t.check(code == 302 and headers.get("Location") == "/Account/Login?sessionExpired=true",
    "stale form rejected and redirected after restart")
t.check(not any(c.name == "TraineeTracker.Auth" for c in browser.jar), "stale form does not authenticate")
t.check(browser.request("/TraineeDashboard")[0] == 302, "stale form grants no dashboard access")
code, body, _ = browser.request(headers["Location"])
t.check(code == 200 and "role=\"alert\"" in body, "fresh form explains expired session")
code, _, headers = browser.request("/Account/Login",
    {"Email": "trainee@demo.example", "Password": "TraineeDemo!2026"})
t.check(code == 302 and headers.get("Location") == "/TraineeDashboard", "fresh form authenticates")
t.check(browser.request("/TraineeDashboard")[0] == 200, "dashboard works with recovered cookie jar")


# Existing authentication, sandbox and antiforgery cookies from the old process must recover on GET.
code, _, _ = authenticated.request("/Account/Login")
t.check(code == 200, "existing old cookie jar obtains a fresh login form")
code, _, headers = authenticated.request("/Account/Login",
    {"Email": "admin@demo.example", "Password": "AdminDemo!2026"})
t.check(code == 302 and headers.get("Location") == "/Admin", "old cookie jar can authenticate again")
authenticated.request("/Admin")
current_token = authenticated.token
authenticated.token = ""
code, _, _ = authenticated.request("/Admin/Create",
    {"FirstName": "Rejected", "LastName": "Example", "Email": "rejected@demo.example",
     "Password": "Synthetic!2026", "Role": "Mentor"})
t.check(code == 400, "non-login mutations still reject missing antiforgery tokens")
authenticated.token = current_token
t.check("rejected@demo.example" not in authenticated.request("/Admin")[1],
    "rejected mutation did not create a user")
