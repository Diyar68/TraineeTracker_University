# Trainee Tracker – Portfolio-Demo

ASP.NET Core MVC mit .NET 10, C#, Entity Framework Core und SQLite.
Die Anwendung entstand als gemeinsames Universitätsprojekt eines studentischen
Teams. Diese bereinigte Portfolio-Fassung zeigt die gemeinsam entwickelte
Anwendung mit einem eigenen, synthetischen Beispielbestand. Sie beansprucht
keine alleinige Urheberschaft am ursprünglichen Teamprojekt.

## Start

.NET-10-SDK installieren, dann im Verzeichnis dieser Kopie ausführen:

    Demo__Enabled=true ASPNETCORE_ENVIRONMENT=Development PORT=10000 dotnet run --no-launch-profile --project TraineeTracker

Unter http://localhost:10000/Account/Login stehen Trainee-Demo und Mentor-Demo
bereit. Der Mentor kann zwischen zwei zugewiesenen fiktiven Trainees wechseln.
Die Demo zeigt Lehrplan, Wochenplan, Fortschritt und Feedback ausschließlich lesend.

Die Portfolio-Fassung startet ausschließlich mit Demo__Enabled=true.
Es gibt keine Passwortliste und keinen Admin-Demo-Zugang.
Alle Anwesenheitswerte werden lokal synthetisch berechnet. Es gibt keine
externe Arbeitszeit-API und keinen E-Mail-Versand.

## Docker und Render

    docker build -t trainee-tracker-portfolio .
    docker run --rm -p 10000:10000 -e Demo__Enabled=true -e ASPNETCORE_ENVIRONMENT=Production -e PORT=10000 -e RENDER=true trainee-tracker-portfolio

Für Render den Docker-Web-Service mit dem Stamm dieser Kopie als Build-Kontext
und ./Dockerfile verwenden. Die Laufzeitvariablen sind:

    Demo__Enabled=true
    ASPNETCORE_ENVIRONMENT=Production
    PORT=10000

Render setzt RENDER=true. Der Health-Check-Pfad ist /healthz.
Kestrel bindet explizit an 0.0.0.0 und PORT. Der Container läuft als appuser.
RENDER=true delegiert die HTTPS-Weiterleitung an den Render-Edge-Proxy.
Production-Cookies tragen Secure und HttpOnly. Der lokale HTTP-Dockeraufruf
ermöglicht Health-Checks. Für die Anmeldung benötigt der Browser HTTPS,
beispielsweise am Render-Edge. Die Tests können dies lokal simulieren.

Jeder Prozess erstellt eine neue SQLite-Datei unter dem temporären
Systemverzeichnis, wendet die Migrationen an und erzeugt den Beispielbestand.
Bestehende Datenbanken werden nicht geöffnet. Alte temporäre Dateien bleiben
bis zur Bereinigung des temporären Dateisystems erhalten. Render Free verwendet
flüchtigen Speicher. Eine erneute Anmeldung nach Instanzwechsel ist vorgesehen.

## Sicherheit und Tests

Alle datenändernden HTTP-Anfragen sind im Demo-Modus zentral gesperrt.
Ausgenommen sind die per Antiforgery geschützten POST-Aktionen für Demo-Anmeldung,
Abmeldung und zugeordnete Trainee-Auswahl. Admin und Lehrplanimport sind gesperrt.
Login-Versuche sind begrenzt. Hinter dem Render-Proxy kann das IP-basierte Limit
mehrere Besucher gemeinsam betreffen.

    dotnet test TraineeTracker.UnitTests

HTTP-Prüfungen stehen unter tests/demo_http.py. Anleitung unter tests/README.md.
Die bisherigen Selenium-Tests für passwortbasierte Anmeldung und schreibende
Admin-Abläufe sind in dieser Fassung nicht enthalten.

## Herkunft und Veröffentlichung

Firmenlogos, der übernommene Unternehmenslehrplan und die universitäre
Deployment-Konfiguration wurden entfernt. Der Demo-Lehrplan enthält allgemeine
Themen ohne übernommene Beschreibungen und externe Lehrplanlinks.

Für den ursprünglichen Teamcode ist keine Projektlizenz dokumentiert.
Diese Kopie erteilt keine zusätzlichen Nutzungsrechte. Die Zustimmung der
Mitwirkenden und etwaige Hochschulvereinbarungen müssen vor einer öffentlichen
Veröffentlichung geklärt werden. Diese Fassung ist zunächst für ein privates
Repository vorgesehen.
