import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8');
const start = source.indexOf('function companyRouteAllowed(');
const end = source.indexOf('export async function sendApiRequest', start);
const context = vm.createContext({});
vm.runInContext(source.slice(start, end), context);
const id = '11111111-1111-4111-8111-111111111111';
for (const [method, path] of [['GET','/api/Companies/capabilities'],['POST','/api/Companies/city-options'],['POST','/api/Companies/query'],['POST','/api/Companies/find'],['POST','/api/Companies/filter-values'],['POST','/api/Companies'],['GET',`/api/Companies/${id}`],['PUT',`/api/Companies/${id}`],['DELETE',`/api/Companies/${id}`]])
    assert.equal(context.companyRouteAllowed(method,path), true, `${method} ${path}`);
for (const [method, path] of [['DELETE','/api/Companies'],['PUT','/api/Companies/city-options'],['GET',`/api/Companies/${id}/password`],['POST',`/api/Companies/${id}/password`],['GET','https://example.test/api/Companies'],['GET','/api/Companies/../Auth'],['PUT','/api/Companies/not-guid/password']])
    assert.equal(context.companyRouteAllowed(method,path), false, `${method} ${path}`);
assert.ok(source.includes('companyRouteAllowed(request.method, request.relativePath) ||'));
console.log('PASS: Companies allowlist, 9 allowed and 7 rejected routes; integrated into sendApiRequest.');
