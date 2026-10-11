import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js', import.meta.url), 'utf8');
const start = source.indexOf('function securityEventRouteAllowed(');
const context = vm.createContext({});
vm.runInContext(source.slice(start,source.indexOf('export async function sendApiRequest',start)),context);
const id = '11111111-1111-4111-8111-111111111111';
for (const [method,path] of [['GET','/api/security-events/capabilities'],['POST','/api/security-events/query'],['POST','/api/security-events/find'],['POST','/api/security-events/filter-values'],['GET',`/api/security-events/${id}`]])
 assert.equal(context.securityEventRouteAllowed(method,path),true);
for (const [method,path] of [['GET','/api/security-events'],['POST','/api/security-events'],['PUT',`/api/security-events/${id}`],['DELETE',`/api/security-events/${id}`],['GET','/api/security-events/query'],['GET','https://example.test/api/security-events/capabilities'],['GET','/api/security-events/../Auth'],['GET','/api/security-events/not-guid']])
 assert.equal(context.securityEventRouteAllowed(method,path),false);
assert.ok(source.includes('securityEventRouteAllowed(request.method, request.relativePath) ||'));
console.log('PASS: Security events routes, 5 allowed, 8 rejected; wired into sendApiRequest.');
