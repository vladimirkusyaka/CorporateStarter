import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const context = vm.createContext({isSecureContext:false, Map, Date, Set});
const mod = new vm.SourceTextModule(fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8'), {context});
await mod.link(() => {throw new Error('Unexpected import');}); await mod.evaluate();
const id = '11111111-1111-4111-8111-111111111111';
const valid = new Map([
    ['/api/Roles', ['GET','POST']], ['/api/Roles/'+id, ['GET','PUT','DELETE']],
    ['/api/Roles/capabilities',['GET']], ['/api/Roles/permission-options',['GET']],
    ...['query','find','filter-values'].map(x=>['/api/Roles/'+x,['POST']])
]);
for (const [path, methods] of valid) for (const method of ['GET','POST','PUT','DELETE']) {
    const reply = await mod.namespace.sendApiRequest({id,method,relativePath:path,expectedUserId:id,timeoutMilliseconds:1000});
    assert.equal(reply.kind, methods.includes(method) ? 'unavailable' : 'invalid_request', method+' '+path);
}
for (const path of ['/api/Roles/../Users','/api/Roles/query?x=1','https://example.com/api/Roles','/api/Roles/not-a-guid']) {
    const reply = await mod.namespace.sendApiRequest({id,method:'GET',relativePath:path,expectedUserId:id,timeoutMilliseconds:1000});
    assert.equal(reply.kind,'invalid_request');
}
console.log('PASS: Roles methods/routes and invalid path rejection.');
