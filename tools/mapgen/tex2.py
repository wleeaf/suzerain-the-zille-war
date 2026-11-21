import sys, UnityPy
D, out = sys.argv[1], sys.argv[2]
for n in sys.argv[3:]:
    env = UnityPy.load(f"{D}/{n}")
    for obj in env.objects:
        if obj.type.name=="Texture2D":
            x=obj.read(); print(x.m_Name,x.m_Width,x.m_Height); x.image.save(f"{out}/{x.m_Name}.png")
