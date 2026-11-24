Run the application with Demo__Enabled=true and ASPNETCORE_ENVIRONMENT=Development
on http://127.0.0.1:5189, then run python3 tests/demo_http.py.

For a Production container with RENDER=true, run
python3 tests/demo_http.py http://127.0.0.1:5190 --proxy.
This checks Secure/HttpOnly on the authentication cookie and emulates the TLS
edge by allowing the test client to send Secure cookies over local HTTP.
