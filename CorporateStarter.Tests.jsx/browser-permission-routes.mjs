import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8');
const context = vm.createContext({isSecureContext:false, Map, Date, Set});
const mod = new vm.SourceTextModule(source, {context});
await mod.link(() => { throw new Error('Unexpected import'); });
await mod.evaluate();
const id = '11111111-1111-4111-8111-111111111111';
async function result(path, method) {
    return (await mod.namespace.sendApiRequest({id, method, relativePath:path, expectedUserId:id, timeoutMilliseconds:1000})).kind;
}
for (const path of ['/api/Permissions', '/api/Permissions/capabilities']) {
    assert.equal(await result(path, 'GET'), 'unavailable', path);
    for (const method of ['POST', 'PUT', 'DELETE']) assert.equal(await result(path, method), 'invalid_request');
}
for (const endpoint of ['query', 'find', 'filter-values']) {
    const path = '/api/Permissions/' + endpoint;
    assert.equal(await result(path, 'POST'), 'unavailable', path);
    for (const method of ['GET', 'PUT', 'DELETE']) assert.equal(await result(path, method), 'invalid_request');
}
for (const path of ['/api/Permissions/' + id, '/api/Permissions/query?x=1', '/api/Permissions/../Users', 'https://example.com/api/Permissions'])
    for (const method of ['GET', 'POST', 'PUT', 'DELETE']) assert.equal(await result(path, method), 'invalid_request', path);
console.log('PASS: Permissions read routes accepted; mutation, traversal and external routes rejected.');
