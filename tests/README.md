# Local regression checks

Run dotnet test TraineeTracker.UnitTests for the original application unit tests
and replacement synthetic-attendance tests.

Run a disposable application instance, then:

    python3 tests/functional_http.py http://127.0.0.1:10000

For Production behind a local simulated Render edge, add --proxy. That mode
checks Secure/HttpOnly cookies and allows the test client to transmit them over
local HTTP. It does not test Render's actual TLS edge.

Browser smoke checks use Node.js with built-in WebSocket support and Chrome.
Start Chrome with a separate temporary profile and a debugging port on 9517.
Run the app in Development on port 5194, then:

    node tests/browser_smoke.mjs

Override BROWSER_TEST_URL if needed. The script captures actual application
screenshots under docs/screenshots and checks three logins, 62 lesson cards,
statistics, weekly plan, mentor view and user administration. No user Chrome
profile is accessed. The former read-only tests have been replaced.

The university-era Selenium tests depend on legacy account credentials and an
older ChromeDriver. This deployment uses the browser and HTTP checks above.
