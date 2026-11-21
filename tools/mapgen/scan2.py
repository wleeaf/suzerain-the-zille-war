import sys, UnityPy, collections
D = sys.argv[1]
env = UnityPy.load(*[f"{D}/{n}" for n in sys.argv[2:]])
scripts = collections.Counter(); names=[]; mb=[]
for obj in env.objects:
    if obj.assets_file.name and "monoscripts" in str(obj.assets_file.parent.name if obj.assets_file.parent else ""): pass
    if obj.type.name == "GameObject":
        names.append(obj.read().m_Name)
    if obj.type.name == "MonoBehaviour":
        try:
            d = obj.read(check_read=False)
            sc = d.m_Script.read().m_ClassName
            scripts[sc] += 1
            mb.append((sc, d.m_Name))
        except Exception as e:
            scripts["ERR "+repr(e)[:80]] += 1
print(scripts.most_common(40))
print([m for m in mb if m[1]][:80])
print(sorted(set(names))[:400])
