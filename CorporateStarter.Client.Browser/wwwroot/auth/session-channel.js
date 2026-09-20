const channelName = "CorporateStarter.auth.notifications.v1";
const messageType = "session-changed";
const senderId = globalThis.crypto.randomUUID();

let sessionListener = null;
let sessionListenerLease = 0;

export function startSessionListener(receiver) {
    if (typeof receiver?.invokeMethodAsync !== "function")
        throw new TypeError("A .NET receiver is required.");

    const listener = createSessionChannel(
        () => receiver.invokeMethodAsync("OnSessionChanged"));

    sessionListener?.dispose();
    sessionListener = listener;
    return ++sessionListenerLease;
}

export function stopSessionListener(lease) {
    if (lease !== sessionListenerLease)
        return;

    sessionListener?.dispose();
    sessionListener = null;
}

export function createSessionChannel(onChanged) {
    if (typeof onChanged !== "function")
        throw new TypeError("onChanged must be a function.");

    if (typeof globalThis.BroadcastChannel !== "function")
        throw new Error("BroadcastChannel is not supported.");

    const channel = new BroadcastChannel(channelName);
    let disposed = false;
    let pending = false;
    let processing = false;
    let retryTimer = null;

    async function deliver() {
        if (disposed || processing || retryTimer !== null)
            return;

        processing = true;

        try {
            while (pending && !disposed) {
                pending = false;

                try {
                    await onChanged();
                }
                catch {
                    if (!disposed) {
                        pending = true;
                        retryTimer = setTimeout(() => {
                            retryTimer = null;
                            void deliver();
                        }, 1000);
                    }

                    break;
                }
            }
        }
        finally {
            processing = false;
        }
    }

    channel.onmessage = event => {
        const message = event.data;

        if (disposed || message?.version !== 1 ||
            message?.type !== messageType ||
            typeof message?.senderId !== "string" ||
            !message.senderId || message.senderId === senderId)
            return;

        pending = true;
        void deliver();
    };

    return {
        notifyChanged() {
            if (disposed)
                return false;

            channel.postMessage({
                version: 1,
                type: messageType,
                senderId
            });

            return true;
        },

        dispose() {
            if (disposed)
                return;

            disposed = true;
            pending = false;
            clearTimeout(retryTimer);
            retryTimer = null;
            channel.onmessage = null;
            channel.close();
        }
    };
}
