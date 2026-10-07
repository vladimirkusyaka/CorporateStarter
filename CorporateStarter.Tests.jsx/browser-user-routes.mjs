import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8');
const start = source.indexOf('function userRouteAllowed(');
const end = source.indexOf('export async function sendApiRequest', start);
const context = vm.createContext({});
vm.runInContext(source.slice(start, end), context);
const id = '11111111-1111-4111-8111-111111111111';
for (const [method, path] of [['GET','/api/Users/capabilities'],['GET','/api/Users/role-options'],['POST','/api/Users/query'],['POST','/api/Users/find'],['POST','/api/Users/filter-values'],['POST','/api/Users'],['GET',`/api/Users/${id}`],['PUT',`/api/Users/${id}`],['DELETE',`/api/Users/${id}`],['PUT',`/api/Users/${id}/password`]])
    assert.equal(context.userRouteAllowed(method,path), true, `${method} ${path}`);
for (const [method, path] of [['DELETE','/api/Users'],['PUT','/api/Users/role-options'],['GET',`/api/Users/${id}/password`],['POST',`/api/Users/${id}/password`],['GET','https://example.test/api/Users'],['GET','/api/Users/../Auth'],['PUT','/api/Users/not-guid/password']])
    assert.equal(context.userRouteAllowed(method,path), false, `${method} ${path}`);
assert.ok(source.includes('userRouteAllowed(request.method, request.relativePath) ||'));
console.log('PASS: Users allowlist, 10 allowed and 7 rejected routes; integrated into sendApiRequest.');
