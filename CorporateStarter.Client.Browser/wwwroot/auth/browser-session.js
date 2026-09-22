const sessionLockName = "CorporateStarter.auth.session";

let accessToken = null;
let accessTokenExpiresAt = 0;
let userId = null;
let sessionOperationInProgress = false;
const csrfCookieName = "__Host-corporate_starter_csrf";
let restorePromise = null;

function clearSession() {
    accessToken = null;
    accessTokenExpiresAt = 0;
    userId = null;
    idlePendingActivity = 0;
}

function result(status, id = null) {
    return { status, userId: id };
}

async function notifySessionChanged() {
    let channel;

    try {
        const { createSessionChannel } =
            await import("./session-channel.js");

        channel = createSessionChannel(() => { });
        channel.notifyChanged();
    }
    catch {
        console.warn("Session change notification could not be sent.");
    }
    finally {
        channel?.dispose();
    }
}

export async function login(loginName, password) {
    if (globalThis.isSecureContext !== true ||
        typeof globalThis.navigator?.locks?.request !== "function") {
        return result("unsupported");
    }

    if (typeof loginName !== "string" || !loginName.trim() ||
        typeof password !== "string" || !password) {
        return result("invalid_input");
    }

    if (sessionOperationInProgress)
        return result("busy");

    sessionOperationInProgress = true;

    try {
        return await navigator.locks.request(sessionLockName, async () => {
            clearSession();

            const response = await fetch("/api/Auth/login", {
                method: "POST",
                mode: "same-origin",
                credentials: "same-origin",
                cache: "no-store",
                redirect: "error",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    login: loginName,
                    password
                })
            });

            if (response.status === 401)
                return result("rejected");

            if (response.status === 429)
                return result("rate_limited");

            if (response.status === 202)
                return result("mfa_required");

            if (response.status !== 200)
                return result("unavailable");

            const loginResult = acceptSession(await response.json());

            if (loginResult.status === "authenticated")
                await notifySessionChanged();

            return loginResult;
        });
    }
    catch {
        clearSession();
        return result("unavailable");
    }
    finally {
        sessionOperationInProgress = false;
    }
}

export function restoreSession() {
    if (restorePromise !== null)
        return restorePromise;

    if (sessionOperationInProgress)
        return Promise.resolve(result("unavailable"));

    sessionOperationInProgress = true;

    restorePromise = restoreCore().finally(() => {
        restorePromise = null;
        sessionOperationInProgress = false;
    });

    return restorePromise;
}

function acceptSession(payload) {
    const expiresAt = typeof payload?.expiresAtUtc === "string"
        ? Date.parse(payload.expiresAtUtc)
        : NaN;

    const id = payload?.user?.id;
    const guidPattern =
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

    if (typeof payload?.accessToken !== "string" ||
        !payload.accessToken.trim() ||
        !Number.isFinite(expiresAt) ||
        expiresAt <= Date.now() ||
        typeof id !== "string" ||
        !guidPattern.test(id) ||
        id === "00000000-0000-0000-0000-000000000000") {
        return result("unavailable");
    }

    accessToken = payload.accessToken;
    accessTokenExpiresAt = expiresAt;
    userId = id;
    idleNextCheck = 0;

    return result("authenticated", userId);
}


async function restoreCore() {
    if (globalThis.isSecureContext !== true ||
        typeof globalThis.navigator?.locks?.request !== "function") {
        clearSession();
        return result("unsupported");
    }

    try {
        return await navigator.locks.request(sessionLockName, async () => {
            clearSession();

            const prefix = csrfCookieName + "=";

            const cookies = document.cookie.split(";")
                .map(value => value.trim())
                .filter(value => value.startsWith(prefix));

            if (cookies.length === 0)
                return result("anonymous");

            if (cookies.length !== 1)
                return result("unavailable");

            const csrfToken = cookies[0].slice(prefix.length);

            if (!/^[A-Za-z0-9_-]{43}$/.test(csrfToken))
                return result("anonymous");

            const response = await fetch("/api/Auth/refresh", {
                method: "POST",
                mode: "same-origin",
                credentials: "same-origin",
                cache: "no-store",
                redirect: "error",
                headers: {
                    "Accept": "application/json",
                    "X-CSRF-TOKEN": csrfToken
                }
            });

            if (response.status === 401 || response.status === 403)
                return result("anonymous");

            if (response.status !== 200)
                return result("unavailable");

            return acceptSession(await response.json());
        });
    }
    catch {
        clearSession();
        return result("unavailable");
    }
}

export async function logout() {
    if (sessionOperationInProgress)
        return result("busy");

    clearSession();

    if (globalThis.isSecureContext !== true ||
        typeof globalThis.navigator?.locks?.request !== "function") {
        return result("unsupported");
    }

    sessionOperationInProgress = true;

    try {
        return await navigator.locks.request(sessionLockName, async () => {
            const prefix = csrfCookieName + "=";

            const cookies = document.cookie.split(";")
                .map(value => value.trim())
                .filter(value => value.startsWith(prefix));

            if (cookies.length !== 1)
                return result("unavailable");

            const csrfToken = cookies[0].slice(prefix.length);

            if (!/^[A-Za-z0-9_-]{43}$/.test(csrfToken))
                return result("unavailable");

            const response = await fetch("/api/Auth/logout", {
                method: "POST",
                mode: "same-origin",
                credentials: "same-origin",
                cache: "no-store",
                redirect: "error",
                headers: {
                    "Accept": "application/json",
                    "X-CSRF-TOKEN": csrfToken
                }
            });

            if (response.status !== 204)
                return result("unavailable");

            await notifySessionChanged();

            return result("signed_out");
        });
    }
    catch {
        return result("unavailable");
    }
    finally {
        sessionOperationInProgress = false;
    }
}


// Idle activity is a separate concern from refresh. Timers never count as activity.
let idleReceiver = null;
let idleLease = 0;
let idleTimer = null;
let idleEvents = null;
let idlePendingActivity = 0;
let idleNextCheck = 0;
let idleLastReport = -Infinity;
let idleChecking = false;
const idleReportInterval = 15_000;

export function startIdleMonitor(receiver) {
    stopIdleMonitor(idleLease);
    const lease = ++idleLease;
    idleReceiver = receiver;
    idleEvents = new AbortController();
    const eventOptions = { capture: true, passive: true, signal: idleEvents.signal };
    const onActivity = event => {
        if (!event.isTrusted || document.visibilityState !== "visible" || userId === null)
            return;
        idlePendingActivity++;
        // A trailing report ensures a final keystroke is not lost to throttling.
        idleNextCheck = Math.min(idleNextCheck, idleLastReport + idleReportInterval);
    };
    for (const name of ["pointerdown", "keydown", "input", "wheel", "touchstart"])
        document.addEventListener(name, onActivity, eventOptions);
    document.addEventListener("visibilitychange", () => {
        // Visibility is a reason to check, not a reason to extend a session.
        idleNextCheck = 0;
    }, eventOptions);
    idleNextCheck = 0;
    scheduleIdleCheck(lease);
    return lease;
}

export function stopIdleMonitor(lease) {
    if (lease !== idleLease) return;
    idleEvents?.abort();
    idleEvents = null;
    clearTimeout(idleTimer);
    idleTimer = null;
    idleReceiver = null;
    idlePendingActivity = 0;
}

function scheduleIdleCheck(lease) {
    idleTimer = setTimeout(async () => {
        try { await checkIdleSession(lease); }
        finally {
            if (idleReceiver !== null && lease === idleLease)
                scheduleIdleCheck(lease);
        }
    }, 1000);
}

async function checkIdleSession(lease) {
    if (idleChecking || idleReceiver === null || lease !== idleLease || userId === null ||
        sessionOperationInProgress || performance.now() < idleNextCheck)
        return;

    idleChecking = true;
    let revalidate = false;
    let requestTimeout;
    try {
        await navigator.locks.request(sessionLockName, async () => {
            if (lease !== idleLease || idleReceiver === null || userId === null) return;
            const activity = idlePendingActivity;
            const prefix = csrfCookieName + "=";
            const cookies = document.cookie.split(";").map(x => x.trim())
                .filter(x => x.startsWith(prefix));
            const csrf = cookies.length === 1 ? cookies[0].slice(prefix.length) : "";
            if (!/^[A-Za-z0-9_-]{43}$/.test(csrf)) {
                revalidate = true;
                return;
            }
            const started = performance.now();
            const abort = new AbortController();
            requestTimeout = setTimeout(() => abort.abort(), 10_000);
            const response = await fetch(activity > 0
                ? "/api/Auth/session/activity" : "/api/Auth/session/status", {
                method: "POST",
                mode: "same-origin",
                credentials: "same-origin",
                cache: "no-store",
                redirect: "error",
                signal: abort.signal,
                headers: {
                    "Accept": "application/json",
                    "X-CSRF-TOKEN": csrf,
                    "X-Session-User": userId
                }
            });
            if (response.status !== 200) {
                revalidate = true;
                return;
            }
            const payload = await response.json();
            const remaining = payload?.remainingMilliseconds;

            const validUser =
                typeof payload?.userId === "string" &&
                payload.userId.toLowerCase() === userId.toLowerCase();

            const idleDisabled = remaining === null;

            const validRemaining =
                idleDisabled ||
                (Number.isFinite(remaining) && remaining > 0);

            if (!validUser || !validRemaining) {
                revalidate = true;
                return;
            }

            idlePendingActivity = Math.max(0, idlePendingActivity - activity);
            idleLastReport = started;

            const nextCheckDelay = idleDisabled
                ? idleReportInterval
                : Math.min(idleReportInterval, remaining);

            idleNextCheck = started + nextCheckDelay;
            // Rotation keeps an active session alive, but never updates user activity.
            if (accessTokenExpiresAt <= Date.now() + 30_000)
                revalidate = true;
        });
    }
    catch { revalidate = true; }
    finally {
        clearTimeout(requestTimeout);
        idleChecking = false;
    }
    if (revalidate && idleReceiver !== null && lease === idleLease) {
        idleNextCheck = performance.now() + idleReportInterval;
        // Restore passes through the server idle check and the coordinator's
        // generation handling; rejected sessions unmount all protected content.
        try { await idleReceiver.invokeMethodAsync("RevalidateIdleSession"); }
        catch { /* A new circuit reattaches a receiver on its first render. */ }
    }
}

// Business API requests. No refresh or automatic retries occur here.
const clientApiRequests = new Map();
const clientApiMaxResponseBytes = 4 * 1024 * 1024;

export async function sendApiRequest(request) {
    const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    const route = /^\/api\/Countries(?:\/(?:query|find|filter-values|capabilities)|\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})?$/i;
    const methods = ["GET", "POST", "PUT", "DELETE"];

    if (!request || typeof request.id !== "string" || !guid.test(request.id) ||
        clientApiRequests.has(request.id) || !methods.includes(request.method) ||
        typeof request.relativePath !== "string" || !route.test(request.relativePath) ||
        typeof request.expectedUserId !== "string" || !guid.test(request.expectedUserId) ||
        request.expectedUserId === "00000000-0000-0000-0000-000000000000" ||
        (request.jsonBody != null && typeof request.jsonBody !== "string") ||
        (request.method === "GET" && request.jsonBody != null) ||
        !Number.isInteger(request.timeoutMilliseconds) ||
        request.timeoutMilliseconds < 1 || request.timeoutMilliseconds > 120_000) {
        return { kind: "invalid_request" };
    }

    if (globalThis.isSecureContext !== true)
        return { kind: "unavailable" };

    if (sessionOperationInProgress)
        return { kind: "busy" };

    if (userId === null || accessToken === null)
        return { kind: "session_required" };

    if (userId.toLowerCase() !== request.expectedUserId.toLowerCase())
        return { kind: "session_changed" };

    if (accessTokenExpiresAt <= Date.now())
        return { kind: "session_required" };

    const tokenAtStart = accessToken;
    const userAtStart = userId;
    const controller = new AbortController();
    let timedOut = false;
    clientApiRequests.set(request.id, controller);
    const timeout = setTimeout(() => {
        timedOut = true;
        controller.abort();
    }, request.timeoutMilliseconds);

    try {
        const headers = {
            "Accept": "application/json",
            "Authorization": `Bearer ${tokenAtStart}`
        };
        if (request.jsonBody != null)
            headers["Content-Type"] = "application/json";

        const response = await fetch(request.relativePath, {
            method: request.method,
            mode: "same-origin",
            credentials: "omit",
            cache: "no-store",
            redirect: "error",
            signal: controller.signal,
            headers,
            body: request.jsonBody ?? undefined
        });

        const body = await readClientApiBody(response);
        if (controller.signal.aborted)
            return { kind: timedOut ? "timeout" : "cancelled" };

        // Rotation also invalidates this result conservatively.
        if (sessionOperationInProgress || userId !== userAtStart || accessToken !== tokenAtStart)
            return { kind: "session_changed" };

        return {
            kind: "response",
            statusCode: response.status,
            body: body.length <= 2_048 ? body : null,
            bodyStream: body.length > 2_048
                ? DotNet.createJSStreamReference(new TextEncoder().encode(body))
                : null,
            contentType: response.headers.get("Content-Type"),
            retryAfter: response.headers.get("Retry-After")
        };
    }
    catch {
        return {
            kind: controller.signal.aborted
                ? (timedOut ? "timeout" : "cancelled")
                : "unavailable"
        };
    }
    finally {
        clearTimeout(timeout);
        clientApiRequests.delete(request.id);
    }
}

export function cancelApiRequest(requestId) {
    clientApiRequests.get(requestId)?.abort();
}

async function readClientApiBody(response) {
    if (response.body === null)
        return "";

    const reader = response.body.getReader();
    const decoder = new TextDecoder("utf-8", { fatal: true });
    let bytes = 0;
    let text = "";
    let completed = false;
    try {
        while (true) {
            const chunk = await reader.read();
            if (chunk.done) {
                text += decoder.decode();
                completed = true;
                return text;
            }
            bytes += chunk.value.byteLength;
            if (bytes > clientApiMaxResponseBytes)
                throw new Error("API response exceeded the size limit.");
            text += decoder.decode(chunk.value, { stream: true });
        }
    }
    finally {
        if (!completed) {
            try { await reader.cancel(); }
            catch { /* Preserve the original read failure. */ }
        }
        reader.releaseLock();
    }
}

