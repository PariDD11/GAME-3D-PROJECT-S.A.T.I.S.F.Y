using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Pintu keluar di dalam interior.</summary>
    public class InteriorExit : Interactable
    {
        public override void Interact() { InteriorRoute.Exit(); }
    }
}
