import json, sys, os, re
db = json.load(open(sys.argv[1])); out = sys.argv[2]
F = lambda o: {f['title']: f['value'] for f in o['fields']}
actors = {str(a['id']): F(a).get('Name', '?') for a in db['actors']}
idx = []
for c in db['conversations']:
    cf = F(c); title = cf.get('Title', str(c['id']))
    lines = [f"# {title}", f"desc: {cf.get('Description','')}", ""]
    for e in sorted(c['dialogueEntries'], key=lambda e: e['id']):
        ef = F(e)
        txt = ef.get('en') or ef.get('Dialogue Text') or ''
        menu = ef.get('Menu Text en') or ef.get('Menu Text') or ''
        who = actors.get(ef.get('Actor', ''), '?')
        links = ','.join(str(l['destinationDialogueID']) if l['destinationConversationID'] == c['id'] else f"c{l['destinationConversationID']}:{l['destinationDialogueID']}" for l in e['outgoingLinks'])
        s = f"[{e['id']}] {who}: {txt}"
        if menu: s += f"\n    MENU: {menu}"
        if e.get('conditionsString'): s += f"\n    IF: {e['conditionsString']}"
        if e.get('userScript'): s += f"\n    DO: {e['userScript']}"
        if links: s += f"\n    -> {links}"
        lines.append(s)
    fn = re.sub(r'[^A-Za-z0-9_.-]+', '_', title)[:150] + f'__c{c["id"]}.txt'
    open(os.path.join(out, fn), 'w').write('\n'.join(lines))
    idx.append(f"{c['id']}\t{len(c['dialogueEntries'])}\t{title}\t{cf.get('Description','')[:200]}")
open(os.path.join(out, '_index.tsv'), 'w').write('\n'.join(idx))
vars_ = [F(v) for v in db['variables']]
open(os.path.join(out, '_variables.tsv'), 'w').write('\n'.join(f"{v.get('Name')}\t{v.get('Initial Value')}\t{v.get('Description','')}" for v in vars_))
