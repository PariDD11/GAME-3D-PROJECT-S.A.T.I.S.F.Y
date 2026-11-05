using UnityEngine;
using HunterGuild;

public static partial class NpcKit
{
    // Model NPC asli dari HGModelConfig (dipilih lewat wizard). Mengembalikan null bila tidak dipakai.
    static GameObject TrySpawnExternal(Transform parent, string name, Vector3 pos, float yaw, Spec s, bool addCollider)
    {
        var cfg = HGModelConfig.Load();
        if (cfg == null || !cfg.HasNpcs) return null;
        int seed = (s.name ?? name).GetHashCode() ^ (s.outfit * 7919) ^ (s.hair * 104729);
        var prefab = cfg.PickNpc(s.race, s.female, seed);
        if (prefab == null) return null;

        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        var model = Object.Instantiate(prefab, root.transform, false);
        model.name = "Model";
        foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        foreach (var rb in model.GetComponentsInChildren<Rigidbody>()) Object.DestroyImmediate(rb);

        // tinggi sesuai ras / gender, variasi kecil per individu
        float h = cfg.npcHeight;
        switch (s.race) { case Dwarf: h *= 0.80f; break; case Elf: h *= 1.05f; break; case Beast: h *= 1.0f; break; }
        if (s.female) h *= 0.95f;
        h *= 1f + ((Mathf.Abs(seed) % 9) - 4) * 0.012f;
        ExternalModelDriver.FitTo(model.transform, h);

        var anim = model.GetComponentInChildren<Animator>();
        if (anim != null && cfg.npcController != null) anim.runtimeAnimatorController = cfg.npcController;

        if (addCollider)
        {
            var cc = root.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, h * 0.5f, 0f); cc.radius = 0.32f; cc.height = h;
            var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true;
        }
        var drv = root.AddComponent<ExternalNpcDriver>();
        drv.Init(anim);

        var actor = root.AddComponent<NpcActor>();
        actor.displayName = s.name;
        actor.race = s.race; actor.outfit = s.outfit; actor.role = s.outfit; actor.female = s.female;
        actor.beastKind = s.race == Beast ? s.beast : -1;
        actor.height = h;
        return root;
    }
}
