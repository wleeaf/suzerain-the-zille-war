# Offline dialogue walker: replays a conversation from a self-test dump (UserData/ZilleWar/convdump/after) with the
# variables of a real save. Usage: dialogue_walk.py <save.json> <moddingkit/save.json> <dump dir> <conv:entry> [choice index]
import re, sys, json, glob, os
SAVE, MODSAVE, DUMPDIR, START = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
pick = int(sys.argv[5]) if len(sys.argv) > 5 else 0
V = {}
raw = json.load(open(SAVE))['variables']
for m in re.finditer(r'\["([^"]+)"\]=("(?:[^"\\]|\\.)*"|true|false|nil|-?[\d.]+(?:e-?\d+)?)', raw):
    k, v = m[1], m[2]
    V[k] = True if v == 'true' else False if v == 'false' else None if v == 'nil' else v[1:-1] if v.startswith('"') else float(v)
for k, v in json.load(open(MODSAVE))['Variables'].items():
    V[k] = v
convs = {}; titles = {}
for f in glob.glob(os.path.join(DUMPDIR, '*.txt')):
    txt = open(f, encoding='utf-8-sig').read()
    hm = re.match(r'# (.*) \(c(\d+)\)', txt); cid = int(hm[2]); titles[cid] = hm[1]
    ents = {}
    for blk in re.split(r'\n(?=\[\d+\])', txt):
        m = re.match(r'\[(\d+)\](?: \{([^}]*)\})? ([^:\n]*): ?(.*)', blk)
        if not m: continue
        e = {'node': m[2], 'who': m[3], 'text': m[4], 'if': '', 'do': '', 'links': [], 'group': False}
        for l in blk.split('\n')[1:]:
            l = l.strip()
            if l.startswith('IF:'): e['if'] = l[3:].strip()
            elif l.startswith('DO:'): e['do'] = l[3:].strip()
            elif l == 'GROUP': e['group'] = True
            elif l.startswith('->'):
                for x in l[2:].strip().split(','):
                    pm = re.match(r'(?:c(\d+):)?(\d+)(?:\((\w+)\))?', x)
                    e['links'].append((int(pm[1]) if pm[1] else cid, int(pm[2]), pm[3] or 'Normal'))
        ents[int(m[1])] = e
    convs[cid] = ents
def lua2py(s):
    s = re.sub(r'Variable\["([^"]+)"\]', r'V.get("\1")', s)
    s = s.replace('~=', '!=').replace('&&', ' and ').replace('||', ' or ')
    s = re.sub(r'\btrue\b', 'True', s); s = re.sub(r'\bfalse\b', 'False', s); s = re.sub(r'\bnil\b', 'None', s)
    return s
def ok(e):
    if not e['if']: return True
    try: return bool(eval(lua2py(e['if']), {'V': V}))
    except Exception as ex: print('   !! cannot eval', e['if'][:120], ex); return True
def run(e):
    for m in re.finditer(r'Variable\["([^"]+)"\]\s*=\s*([^;]+);', e['do']):
        try: V[m[1]] = eval(lua2py(m[2]), {'V': V})
        except Exception: pass
PRI = {'VeryHigh': 4, 'High': 3, 'AboveNormal': 2.5, 'Normal': 2, 'BelowNormal': 1.5, 'Low': 1, 'VeryLow': 0}
def children(c, i):
    # Dialogue System: groups pass through; highest priority level with any valid entry wins
    e = convs[c][i]; out = []
    best = None
    for (dc, di, pr) in sorted(e['links'], key=lambda l: -PRI[l[2]]):
        if dc not in convs: out.append(((dc, di), 'MISSING CONV')); continue
        d = convs[dc].get(di)
        if d is None: continue
        if best is not None and PRI[pr] < best: break
        if ok(d):
            if d['group']:
                run(d)
                sub = children(dc, di)
                if sub: best = PRI[pr]; out += sub
            else:
                best = PRI[pr]; out.append(((dc, di), d))
    return out
c, i = int(START.split(':')[0]), int(START.split(':')[1])
cur = (c, i); steps = 0
run(convs[c][i])
while steps < 400:
    steps += 1
    ch = [x for x in children(*cur) if x[1] != 'MISSING CONV']
    if not ch:
        print(f'END after {steps} steps at c{cur[0]}:{cur[1]} ({titles.get(cur[0])})'); break
    players = [x for x in ch if x[1]['who'].startswith('Player')]
    if players and len(players) > 1:
        print('   MENU:', ' | '.join(f"[{p[0][1]}{'*' if p[1]['node'] else ''}] {p[1]['text'][:50]}" for p in players))
        nxt = players[min(pick, len(players) - 1)]
    else:
        nxt = ch[0]
    cur = nxt[0]; d = nxt[1]; run(d)
    if d['text'] or d['node']:
        print(f"c{cur[0]}:{cur[1]} {('{'+d['node']+'} ') if d['node'] else ''}{d['who']}: {d['text'][:110]}")
