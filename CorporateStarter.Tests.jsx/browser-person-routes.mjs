import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8');
const start = source.indexOf('function personRouteAllowed(');
const end = source.indexOf('export async function sendApiRequest', start);
const context = vm.createContext({});
vm.runInContext(source.slice(start, end), context);
const id = '11111111-1111-4111-8111-111111111111';
for (const [method, path] of [['GET','/api/Persons/capabilities'],['POST','/api/Persons/company-options'],['POST','/api/Persons/position-options'],['POST','/api/Persons/query'],['POST','/api/Persons/find'],['POST','/api/Persons/filter-values'],['POST','/api/Persons'],['GET',`/api/Persons/${id}`],['PUT',`/api/Persons/${id}`],['DELETE',`/api/Persons/${id}`]])
    assert.equal(context.personRouteAllowed(method,path), true, `${method} ${path}`);
for (const [method, path] of [['DELETE','/api/Persons'],['PUT','/api/Persons/company-options'],['GET',`/api/Persons/${id}/password`],['POST',`/api/Persons/${id}/password`],['GET','https://example.test/api/Persons'],['GET','/api/Persons/../Auth'],['PUT','/api/Persons/not-guid/password']])
    assert.equal(context.personRouteAllowed(method,path), false, `${method} ${path}`);
assert.ok(source.includes('personRouteAllowed(request.method, request.relativePath) ||'));
console.log('PASS: Persons allowlist, 10 allowed and 7 rejected routes; integrated into sendApiRequest.');
