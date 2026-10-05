import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';

const source = fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8');
const uid = '11111111-1111-4111-8111-111111111111';
const deferred = () => { let resolve; const promise = new Promise(r => resolve = r); return {promise, resolve}; };
const session = () => ({status:200, json:async () => ({accessToken:'test-token',expiresAtUtc:new Date(Date.now()+300000).toISOString(),user:{id:uid}})});

async function harness() {
    let tail = Promise.resolve(), tick, notifications = 0;
    const calls = [], events = {}, holds = new Map();
    const context = vm.createContext({
        console, Date, AbortController, AbortSignal, performance:{now:()=>16000},
        isSecureContext:true,
        navigator:{locks:{request:(_name, fn) => { const work = tail.then(fn); tail = work.catch(()=>{}); return work; }}},
        document:{visibilityState:'visible',cookie:'__Host-corporate_starter_csrf='+'a'.repeat(43),addEventListener:(name, fn)=>events[name]=fn},
        setTimeout:(fn, delay)=>{ if(delay===1000) tick=fn; return 1; }, clearTimeout:()=>{},
        fetch:async (url, options)=>{
            calls.push(url);
            assert.ok(options.signal || url.endsWith('/session/status') || url.endsWith('/session/activity'));
            const hold = holds.get(url);
            if (hold) { hold.started.resolve(); return hold.reply.promise; }
            if(url.endsWith('/logout'))return {status:204};
            if(url.includes('/session/'))return {status:200,json:async()=>({userId:uid,remainingMilliseconds:1800000})};
            return session();
        }
    });
    const channel = new vm.SyntheticModule(['createSessionChannel'], function() {
        this.setExport('createSessionChannel',()=>({notifyChanged(){},dispose(){}}));
    }, {context});
    await channel.link(()=>{}); await channel.evaluate();
    const module = new vm.SourceTextModule(source,{context,importModuleDynamically:async()=>channel});
    await module.link(()=>{}); await module.evaluate();
    const api = module.namespace;
    await api.login('test','test'); calls.length=0;
    function hold(path) { const h={started:deferred(),reply:deferred()}; holds.set(path,h); return h; }
    return {api,calls,events,hold,holds,
        start:()=>api.startIdleMonitor({invokeMethodAsync:async()=>{notifications++;}}),
        tick:()=>tick(), notifications:()=>notifications};
}

// A running rotation must finish, but its result cannot reauthenticate after logout intent.
{
    const h=await harness(), blocked=h.hold('/api/Auth/refresh');
    const refresh=h.api.restoreSession();
    assert.equal(refresh,h.api.restoreSession());
    await blocked.started.promise;
    const logout=h.api.logout();
    assert.equal(logout,h.api.logout());
    assert.equal((await h.api.restoreSession()).status,'logout_pending');
    blocked.reply.resolve(session());
    assert.equal((await refresh).status,'unavailable');
    assert.equal((await logout).status,'signed_out');
    assert.deepEqual(h.calls,['/api/Auth/refresh','/api/Auth/logout']);
    console.log('PASS: refresh + logout, shared promises, late refresh rejected');
}

// Activity already in flight may finish; it must not request restoration after logout.
{
    const h=await harness(); const lease=h.start(); h.events.keydown({isTrusted:true});
    const blocked=h.hold('/api/Auth/session/activity'); const activity=h.tick();
    await blocked.started.promise;
    const logout=h.api.logout();
    blocked.reply.resolve({status:200,json:async()=>({userId:uid,remainingMilliseconds:1800000})});
    await activity; await logout;
    assert.equal(h.notifications(),0);
    assert.deepEqual(h.calls,['/api/Auth/session/activity','/api/Auth/logout']);
    await h.tick(); assert.equal(h.calls.length,2);
    h.api.stopIdleMonitor(lease);
    console.log('PASS: activity + logout, stale response ignored, no activity after exit');
}

// Failed logout keeps recovery blocked until an explicit successful retry.
{
    const h=await harness(), blocked=h.hold('/api/Auth/logout');
    const logout=h.api.logout(); await blocked.started.promise;
    blocked.reply.resolve({status:503});
    assert.equal((await logout).status,'unavailable');
    assert.equal((await h.api.restoreSession()).status,'logout_pending');
    assert.equal((await h.api.login('test','test')).status,'busy');
    h.holds.delete('/api/Auth/logout');
    assert.equal((await h.api.logout()).status,'signed_out');
    assert.deepEqual(h.calls,['/api/Auth/logout','/api/Auth/logout']);
    console.log('PASS: uncertain logout blocks automatic restore; explicit retry works');
}

// Activity and rotation serialize on the same browser lock.
{
    const h=await harness(); const lease=h.start(); h.events.keydown({isTrusted:true});
    const blocked=h.hold('/api/Auth/session/activity'); const activity=h.tick();
    await blocked.started.promise; const refresh=h.api.restoreSession();
    assert.deepEqual(h.calls,['/api/Auth/session/activity']);
    blocked.reply.resolve({status:200,json:async()=>({userId:uid,remainingMilliseconds:1800000})});
    await activity; assert.equal((await refresh).status,'authenticated');
    assert.deepEqual(h.calls,['/api/Auth/session/activity','/api/Auth/refresh']);
    h.api.stopIdleMonitor(lease);
    console.log('PASS: activity + refresh are serialized');
}
