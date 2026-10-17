"""Optional live Windows acceptance runner (Python standard library + sqlcmd).
Uses synthetic accounts, keeps cookies/passwords in memory, cleans only this run's
accounts and their data from the explicitly selected local demonstration database.
Never point this runner at a production service or database.
"""
import argparse
import datetime as dt
import http.cookiejar
import json
from pathlib import Path
import secrets
import ssl
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--base-url", default="https://localhost:7142")
parser.add_argument("--server", default=r".\SQLEXPRESS")
parser.add_argument("--database", default="RaceDayPart2")
parser.add_argument("--timeout", type=int, default=120)
parser.add_argument("--output", default="TestResults/acceptance-http.json")
args = parser.parse_args()
url = urllib.parse.urlparse(args.base_url)
if url.scheme != "https" or url.hostname not in {"localhost", "127.0.0.1", "::1"} or url.username or url.password:
    parser.error("Use an HTTPS loopback demonstration API.")
if args.server not in {r".\SQLEXPRESS", r"localhost\SQLEXPRESS", r"(localdb)\MSSQLLocalDB"}:
    parser.error("Only local SQL Server demonstration instances are supported.")
if not args.database.replace("_", "").isalnum() or args.database.lower() in {"master", "model", "msdb", "tempdb"}:
    parser.error("Choose a demonstration database, not a system database.")
context = ssl._create_unverified_context()  # Only for the validated loopback development URL.
allowed = {"userId", "role", "eventId", "organiserId", "eventTypeId", "name", "distanceKm", "eventDate",
           "categories", "categoryId", "categoryType", "minAge", "maxAge", "enrolmentId", "enrolmentStatus",
           "participantId", "resultId", "finishTime", "finishingPosition", "status", "title"}
def sanitized(value):
    if isinstance(value, dict):
        return {k: sanitized(v) for k, v in value.items() if k in allowed}
    if isinstance(value, list):
        return [sanitized(v) for v in value]
    return value

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        return None

def browser():
    return urllib.request.build_opener(urllib.request.ProxyHandler({}),
        NoRedirect(),
        urllib.request.HTTPSHandler(context=context),
        urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

rows = []
actors = []
registered = []
def require(condition, message):
    if not condition:
        raise AssertionError(message)

run_id = uuid.uuid4().hex
password = secrets.token_urlsafe(32)
emails = [f"{role}-{run_id}@raceday.test" for role in ("owner", "other", "participant")]
result = {"startedUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "baseUrl": args.base_url,
          "demoRunId": run_id,
          "transport": "real HTTPS / Kestrel", "checks": rows, "passed": False, "cleanup": "not needed"}
def check(actor, operation, method, path, expected, body=None):
    request = urllib.request.Request(args.base_url.rstrip("/") + path, method=method,
        data=None if body is None else json.dumps(body).encode(),
        headers={} if body is None else {"Content-Type": "application/json"})
    try:
        response = actor.open(request, timeout=args.timeout)
    except urllib.error.HTTPError as ex:
        response = ex
    except (urllib.error.URLError, TimeoutError, OSError) as ex:
        rows.append({"operation": operation, "method": method, "path": path,
                     "status": None, "response": None, "error": type(ex).__name__, "passed": False})
        print(f"{operation}: no HTTP response ({type(ex).__name__})", flush=True)
        raise RuntimeError("Transport failure; no HTTP status was received.") from None
    with response:
        status = response.status
        try:
            raw = response.read().decode()
        except (TimeoutError, OSError, UnicodeError) as ex:
            rows.append({"operation": operation, "method": method, "path": path,
                         "status": status, "response": None, "error": type(ex).__name__, "passed": False})
            print(f"{operation}: HTTP {status}; response body unavailable", flush=True)
            raise RuntimeError("HTTP status received, but response body could not be read.") from None
    non_json = False
    try:
        data = json.loads(raw) if raw and not path.startswith("/swagger") else None
    except json.JSONDecodeError:
        data, non_json = None, True  # Never print an unexpected HTML or diagnostic body.
    rows.append({"operation": operation, "method": method, "path": path, "status": status,
                 "response": sanitized(data), "expected": expected, "passed": status == expected and not non_json})
    print(f"{operation}: HTTP {status} {json.dumps(sanitized(data), separators=(',', ':'))}", flush=True)
    if status != expected:
        raise AssertionError(f"{operation}: expected {expected}, received {status}")
    if non_json:
        raise RuntimeError("Non-JSON API response; body omitted from evidence.")
    return data

def cleanup():
    if not registered:
        return
    # Verify API-returned IDs against exact fresh synthetic emails before any deletion.
    guards = " OR ".join(f"(UserId={uid} AND Email='{email}')" for uid, email in registered)
    ids = ",".join(str(uid) for uid, _ in registered)
    sql = f"""
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF (SELECT COUNT(*) FROM Users WHERE {guards}) <> {len(registered)}
    THROW 51000, 'Cleanup identity verification failed; transaction rolled back.', 1;
IF EXISTS (SELECT 1 FROM Events WHERE OrganiserId IN ({ids}) AND Name <> 'Acceptance {run_id}')
    THROW 51001, 'Unexpected event owned by demo user; transaction rolled back.', 1;
DELETE r FROM Results r JOIN Enrolments en ON en.EnrolmentId=r.EnrolmentId
 JOIN Events e ON e.EventId=en.EventId WHERE e.OrganiserId IN ({ids});
DELETE en FROM Enrolments en JOIN Events e ON e.EventId=en.EventId WHERE e.OrganiserId IN ({ids});
DELETE c FROM Categories c JOIN Events e ON e.EventId=c.EventId WHERE e.OrganiserId IN ({ids});
DELETE FROM Events WHERE OrganiserId IN ({ids});
DELETE FROM Users WHERE {guards};
IF EXISTS (SELECT 1 FROM Users WHERE UserId IN ({ids}))
    THROW 51002, 'Demo users remain; transaction rolled back.', 1;
COMMIT;
SELECT 'Demo cleanup verified' AS Cleanup;
"""
    completed = subprocess.run(["sqlcmd", "-S", args.server, "-E", "-C", "-d", args.database,
                                "-b", "-l", "30", "-t", "60", "-Q", sql],
                               capture_output=True, text=True, timeout=120)
    if completed.returncode or "Demo cleanup verified" not in completed.stdout:
        raise RuntimeError("SQL cleanup failed; inspect the local demo database. No credentials were logged.")
    result["cleanup"] = f"verified: removed {len(registered)} synthetic users and their demo event data"
    print(result["cleanup"], flush=True)

try:
    anonymous = browser()
    check(anonymous, "Swagger UI", "GET", "/swagger/index.html", 200)
    check(anonymous, "event types", "GET", "/api/event-types", 200)
    for email, role in zip(emails, ("Organiser", "Organiser", "Participant")):
        actor = browser()
        actors.append(actor)
        profile = check(actor, "register " + role, "POST", "/api/auth/register", 201,
                        dict(firstName="Demo", lastName="Acceptance", email=email, password=password, role=role))
        user_id = profile.get("userId") if isinstance(profile, dict) else None
        if type(user_id) is not int or user_id <= 0:
            result["cleanup"] = "Registration returned no valid user ID; inspect the synthetic demoRunId before cleanup."
            raise RuntimeError("Registration returned no valid positive integer user ID.")
        registered.append((user_id, email))
        require(profile.get("role") == role, "Registration returned an unexpected role.")
        check(actor, "registration does not authenticate", "GET", "/api/users/me", 401)
        check(actor, "login " + role, "POST", "/api/auth/login", 200, dict(email=email, password=password))
    owner, other, participant = actors
    check(anonymous, "invalid credentials", "POST", "/api/auth/login", 401,
          dict(email=emails[0], password=secrets.token_urlsafe(32)))
    check(anonymous, "invalid credentials do not authenticate", "GET", "/api/users/me", 401)
    event = dict(name=f"Acceptance {run_id}", description="Temporary acceptance demonstration",
                 location="Demo venue", distanceKm=10, eventTypeId=1,
                 eventDate=(dt.datetime.now(dt.timezone.utc)+dt.timedelta(days=2)).isoformat())
    created = check(owner, "create event", "POST", "/api/events", 201, event)
    route = f'/api/events/{created["eventId"]}'
    distance = dict(name="10km Open", categoryType="Distance", distanceKm=10)
    category = check(owner, "create Distance category", "POST", route+"/categories", 201, distance)
    age = check(owner, "create Age category", "POST", route+"/categories", 201,
                dict(name="Adults", categoryType="Age", minAge=18, maxAge=80))
    enrol = check(participant, "enrol participant", "POST", route+"/enrolments", 201,
                  dict(categoryId=category["categoryId"]))
    check(participant, "duplicate enrolment", "POST", route+"/enrolments", 409,
          dict(categoryId=category["categoryId"]))
    replacement = dict(event, name="Unauthorised replacement", distanceKm=5)
    for actor, label in ((participant, "participant"), (other, "other organiser")):
        check(actor, label+" cannot modify event", "PUT", route, 403, replacement)
        check(actor, label+" cannot delete event", "DELETE", route, 403)
    check(other, "other organiser cannot modify category", "PUT",
          f'/api/categories/{category["categoryId"]}', 403, dict(distance, name="Denied", distanceKm=5))
    check(other, "other organiser cannot delete unused category", "DELETE",
          f'/api/categories/{age["categoryId"]}', 403)
    check(other, "other organiser cannot read enrolments", "GET", route+"/enrolments", 403)
    check(other, "other organiser cannot read results", "GET", route+"/results", 403)
    unchanged = check(anonymous, "event unchanged after denied writes", "GET", route, 200)
    require(all(unchanged[k] == created[k] for k in ("name", "distanceKm", "eventTypeId", "eventDate")), "Event state differs from its create response after denied writes.")
    require(len(unchanged["categories"]) == 2, "Denied deletion changed the category list.")
    require(next(c for c in unchanged["categories"] if c["categoryId"] == category["categoryId"])["name"] == "10km Open", "Denied update changed the category name.")
    owned_enrolments = check(owner, "owner reads enrolments", "GET", route+"/enrolments", 200)
    require(len(owned_enrolments) == 1 and owned_enrolments[0]["enrolmentId"] == enrol["enrolmentId"], "The owner's enrolment list differs from the created enrolment.")
    result_route = f'/api/enrolments/{enrol["enrolmentId"]}/result'
    body = dict(finishTime="00:52:18", finishingPosition=47)
    check(owner, "result before start", "POST", result_route, 409, body)
    past = dict(event, eventDate=(dt.datetime.now(dt.timezone.utc)-dt.timedelta(days=1)).isoformat())
    check(owner, "move demo event start into past", "PUT", route, 200, past)
    check(participant, "participant cannot record result", "POST", result_route, 403, body)
    check(other, "other organiser cannot record result", "POST", result_route, 403, body)
    recorded = check(owner, "owner records result", "POST", result_route, 201, body)
    check(owner, "duplicate result", "POST", result_route, 409, body)
    correction = dict(finishTime="00:51:59", finishingPosition=46)
    correction_route = f'/api/results/{recorded["resultId"]}'
    check(other, "other organiser cannot correct result", "PUT", correction_route, 403, correction)
    before = check(owner, "result unchanged after denied correction", "GET", route+"/results", 200)
    require(len(before) == 1 and before[0]["finishTime"] == "00:52:18" and before[0]["finishingPosition"] == 47, "Denied correction changed the recorded result.")
    check(owner, "owner corrects result", "PUT", correction_route, 200, correction)
    history = check(participant, "participant reads corrected history", "GET", "/api/users/me/results", 200)
    require(len(history) == 1 and history[0]["resultId"] == recorded["resultId"], "Participant history does not contain the expected result.")
    require(history[0]["finishTime"] == "00:51:59" and history[0]["finishingPosition"] == 46, "Participant history does not reflect the owner's correction.")
    for actor in actors:
        check(actor, "logout", "POST", "/api/auth/logout", 204)
        check(actor, "protected profile after logout", "GET", "/api/users/me", 401)
    result["passed"] = True
except Exception as ex:
    result["error"] = str(ex) or type(ex).__name__
    print(result["error"], file=sys.stderr, flush=True)
finally:
    if registered:
        # Revoke sessions if a failure interrupted the normal, recorded logout checks.
        if not result["passed"]:
            for actor in actors:
                try:
                    req = urllib.request.Request(args.base_url.rstrip("/")+"/api/auth/logout", method="POST")
                    with actor.open(req, timeout=10):
                        pass
                except Exception:
                    pass
        try:
            cleanup()
        except Exception as ex:
            result["cleanup"] = str(ex)
            result["passed"] = False
            print(str(ex), file=sys.stderr, flush=True)
    result["finishedUtc"] = dt.datetime.now(dt.timezone.utc).isoformat()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2)+"\n", encoding="utf-8")
sys.exit(0 if result["passed"] else 1)
