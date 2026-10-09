import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8');
const start = source.indexOf('function changeRouteAllowed(');
const context = vm.createContext({});
vm.runInContext(source.slice(start,source.indexOf('export async function sendApiRequest',start)),context);
const id = '11111111-1111-4111-8111-111111111111';
for (const [method,path] of [['GET','/api/Audit/capabilities'],['POST','/api/Audit/query'],['POST','/api/Audit/find'],['POST','/api/Audit/filter-values'],['GET',`/api/Audit/${id}`]])
 assert.equal(context.changeRouteAllowed(method,path),true);
for (const [method,path] of [['GET','/api/Audit'],['POST','/api/Audit'],['PUT',`/api/Audit/${id}`],['DELETE',`/api/Audit/${id}`],['GET','/api/Audit/query'],['GET','https://example.test/api/Audit/capabilities'],['GET','/api/Audit/../Auth'],['GET','/api/Audit/not-guid']])
 assert.equal(context.changeRouteAllowed(method,path),false);
assert.ok(source.includes('changeRouteAllowed(request.method, request.relativePath) ||'));
console.log('PASS: Changes routes, 5 allowed, 8 rejected; wired into sendApiRequest.');
