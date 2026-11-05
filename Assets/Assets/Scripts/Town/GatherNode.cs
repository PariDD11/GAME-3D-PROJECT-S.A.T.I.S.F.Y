using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Titik gathering (Herb, Mushroom, Berry). Tumbuh lagi setelah beberapa menit.</summary>
    public class GatherNode : Interactable
    {
        public static readonly List<GatherNode> Nodes = new List<GatherNode>();

        public string itemId = "herb";
        public int minAmount = 1, maxAmount = 2;
        public float respawnSeconds = 240f;
        public GameObject visual;

        protected override void OnEnable() { base.OnEnable(); Nodes.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Nodes.Remove(this); }

        public override void Interact()
        {
            var gs = GameState.I;
            var se = GameCalendar.Current;
            if (!GameCalendar.Grows(itemId, se)) { TownUI.I.Toast(ItemDB.Get(itemId).name + " tidak tumbuh di " + GameCalendar.Name(se) + "."); return; }
            int n = Random.Range(minAmount, maxAmount + 1) + GameCalendar.GatherBonus(se) + SeasonEvents.GatherExtra;
            if (!gs.HasSpace(itemId, n)) { TownUI.I.Toast("Tas penuh! Buang sesuatu dulu (Tab)."); return; }
            gs.AddItem(itemId, n);
            Audio.Sfx("gather", transform.position);
            TownUI.I.Toast("Mengumpulkan " + ItemDB.Get(itemId).name + " x" + n);
            for (int i = 0; i < n; i++) HuntManager.Report("gather", itemId);

            if (visual != null) visual.SetActive(false);
            StartCoroutine(Regrow());
            enabled = false;
        }

        IEnumerator Regrow()
        {
            yield return new WaitForSeconds(respawnSeconds);
            if (visual != null) visual.SetActive(GameCalendar.Grows(itemId, GameCalendar.Current));
            enabled = true;
        }
    }
}
