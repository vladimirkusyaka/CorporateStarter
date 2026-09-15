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
}

function result(status, id = null) {
    return { status, userId: id };
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

            return acceptSession(await response.json());
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

            return result(response.status === 204
                ? "signed_out"
                : "unavailable");
        });
    }
    catch {
        return result("unavailable");
    }
    finally {
        sessionOperationInProgress = false;
    }
}
