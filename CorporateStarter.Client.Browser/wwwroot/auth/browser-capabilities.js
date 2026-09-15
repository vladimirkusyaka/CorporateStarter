export function inspectCapabilities() {
    return {
        isSecureContext: globalThis.isSecureContext === true,
        hasWebLocks:
            typeof globalThis.navigator?.locks?.request === "function"
    };
}