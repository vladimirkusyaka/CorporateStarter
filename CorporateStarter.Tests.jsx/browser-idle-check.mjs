import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';

const source=fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js',import.meta.url),'utf8');
const uid='11111111-1111-4111-8111-111111111111';
let now=0, timer, calls=[], events={}, notifications=0, nextStatus=200;
let remainingMilliseconds = 1800000;
let accessLifetime = 300000;
const context=vm.createContext({
 console, Date, AbortController, AbortSignal, performance:{now:()=>now},
 isSecureContext:true,
 navigator:{locks:{request:async(_name,fn)=>fn()}},
 document:{visibilityState:'visible',cookie:'__Host-corporate_starter_csrf='+'a'.repeat(43),
  addEventListener(name,handler){events[name]=handler;}},
 setTimeout(fn){timer=fn;return 1;},clearTimeout(){timer=null;},
 fetch:async(url,opts)=>{
  calls.push({url,opts});
  if(url.endsWith('/login'))return {status:200,json:async()=>({accessToken:'private-token',expiresAtUtc:new Date(Date.now()+accessLifetime).toISOString(),user:{id:uid}})};
  if(url.endsWith('/logout'))return {status:204};
  return {status:nextStatus,json:async()=>({userId:uid,remainingMilliseconds})};
 }
});
const module=new vm.SourceTextModule(source,{context});await module.link(()=>{});await module.evaluate();
const api=module.namespace;
await api.login('user','password');calls=[];
const lease=api.startIdleMonitor({invokeMethodAsync:async()=>{notifications++;}});
await timer();assert.equal(calls.at(-1).url,'/api/Auth/session/status');
now=16000;await timer();assert.equal(calls.at(-1).url,'/api/Auth/session/status');
assert.equal(calls.filter(x=>x.url.endsWith('/activity')).length,0,'Timer must not extend session');
events.pointerdown({isTrusted:false});now=32000;await timer();assert.equal(calls.at(-1).url,'/api/Auth/session/status');
events.keydown({isTrusted:true});now=48000;await timer();assert.equal(calls.at(-1).url,'/api/Auth/session/activity');
assert.equal(calls.at(-1).opts.headers.Authorization,undefined);
assert.equal(calls.at(-1).opts.headers['X-Session-User'],uid);
assert.equal(calls.at(-1).opts.headers['X-CSRF-TOKEN'],'a'.repeat(43));
now=64000;await timer();assert.equal(calls.at(-1).url,'/api/Auth/session/status','No periodic activity after one key');
context.document.visibilityState='hidden';events.keydown({isTrusted:true});now=80000;await timer();assert.equal(calls.at(-1).url,'/api/Auth/session/status');
context.document.visibilityState='visible';events.visibilitychange({isTrusted:true});await timer();assert.equal(calls.at(-1).url,'/api/Auth/session/status','Visibility must not extend');
nextStatus=401;now=96000;await timer();assert.equal(notifications,1,'Expired session must revalidate UI');
nextStatus=503;now=112000;await timer();assert.equal(notifications,2,'Network failure must revalidate UI');
nextStatus = 200;
remainingMilliseconds = null;
let expectedNotifications = notifications;
for (let i = 0; i < 2; i++) {
    now += 16000;
    await timer();
    assert.equal(notifications, expectedNotifications,
        'Disabled idle timeout must not trigger revalidation');
}
for (const invalid of [undefined, 0, -1, "30000"]) {
    remainingMilliseconds = invalid;
    now += 16000;
    await timer();
    assert.equal(notifications, ++expectedNotifications,
        'Malformed duration must trigger revalidation');
}
remainingMilliseconds = null;
accessLifetime = 10000;
await api.login('user', 'password');
now += 16000;
await timer();
assert.equal(notifications, ++expectedNotifications,
    'Access token must still refresh when idle timeout is disabled');
await api.logout();const count=calls.length;now+=16000;await timer();assert.equal(calls.length,count,'No reports after logout');
api.stopIdleMonitor(lease);assert.equal(timer,null);
console.log('PASS: idle/status, trusted activity only, throttling, hidden tab, visibility, expiry/failure callback, token isolation, logout cleanup.');
