export function deviceTimeZone() { return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC"; }
