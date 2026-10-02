import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source=fs.readFileSync(new URL('../CorporateStarter.Client.Browser/wwwroot/auth/browser-session.js',import.meta.url),'utf8');
const context=vm.createContext({isSecureContext:false, Map, Date, Set});
const mod=new vm.SourceTextModule(source,{context});
await mod.link(()=>{throw new Error('Unexpected import');});
await mod.evaluate();
const id='11111111-1111-4111-8111-111111111111';
async function result(path,method='POST') {
 return (await mod.namespace.sendApiRequest({id,method,relativePath:path,expectedUserId:id,timeoutMilliseconds:1000})).kind;
}
for(const path of ['/api/Cities/query','/api/Cities/find','/api/Cities/filter-values','/api/Cities/country-options','/api/Cities','/api/Cities/'+id,'/api/Countries/query','/api/Positions/query'])
 assert.equal(await result(path),'unavailable',path);
assert.equal(await result('/api/Cities/capabilities','GET'),'unavailable');
for(const path of ['/api/Users','https://example.com/api/Cities','/api/Cities/country-options?x=1','/api/Cities/../Users','/api/Countries/country-options'])
 assert.equal(await result(path),'invalid_request',path);
assert.equal(await result('/api/Cities/country-options','GET'),'invalid_request');
console.log('Browser route allowlist checks passed.');
