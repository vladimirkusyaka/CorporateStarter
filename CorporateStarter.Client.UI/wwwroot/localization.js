let bootTexts;
let initialization;
const key = "corporatestarter.language";
const supported = ["en-US", "de-DE", "ru-RU"];
export function read() {
    let saved;
    try { saved = localStorage.getItem(key); } catch { }
    if (!supported.includes(saved)) {
        try { saved = document.cookie.split("; ").find(x => x.startsWith(key + "="))?.split("=")[1]; } catch { }
    }
    if (supported.includes(saved)) return saved;
    for (const language of navigator.languages ?? [navigator.language]) {
        const prefix = language?.split("-")[0].toLowerCase();
        if (prefix === "en") return "en-US";
        if (prefix === "de") return "de-DE";
        if (prefix === "ru") return "ru-RU";
    }
    return "en-US";
}
export function initialize() { return initialization ??= initializeCore(); }
async function initializeCore() {
    const language = read();
    write(language);
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 5000);
    try {
        const response = await fetch("./_content/CorporateStarter.Client.UI/localization-boot.json?v=20261017", { signal: controller.signal });
        if (response.ok) bootTexts = await response.json();
    } catch { } finally { clearTimeout(timer); }
    write(language);
    return language;
}
export function write(language) {
    if (!supported.includes(language)) return;
    document.documentElement.lang = language;
    for (const element of document.querySelectorAll("[data-cs-text]")) {
        const key = element.getAttribute("data-cs-text");
        element.textContent = bootTexts?.[language]?.[key] ?? key;
    }
    for (const element of document.querySelectorAll("[data-cs-label]")) {
        const key = element.getAttribute("data-cs-label");
        element.setAttribute("aria-label", bootTexts?.[language]?.[key] ?? key);
    }
    try { localStorage.setItem(key, language); } catch { }
    try { document.cookie = `${key}=${language}; Path=/; Max-Age=31536000; SameSite=Lax${location.protocol === "https:" ? "; Secure" : ""}`; } catch { }
}
