using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HunterGuild
{
    /// <summary>Wrapper input: Input System baru maupun Input Manager lama; tombol bisa diganti (Bindings) dan gamepad didukung (Input System).</summary>
    public static class TownInput
    {
#if ENABLE_INPUT_SYSTEM
        static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
        static readonly Act[] SkillActs = { Act.Skill1, Act.Skill2, Act.Skill3, Act.Skill4, Act.Skill5, Act.Skill6, Act.Skill7 };

        static bool Down(Key key) { var k = Keyboard.current; return k != null && k[key].wasPressedThisFrame; }
        static bool Down(Act a) { return Down(Bindings.Get(a)); }
        static bool Held(Act a) { var k = Keyboard.current; return k != null && k[Bindings.Get(a)].isPressed; }
        public static bool HasPad { get { return PadInput.Connected; } }

        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
            if (!Bindings.Capturing)
            {
                v.x = (Held(Act.Right) ? 1f : 0f) - (Held(Act.Left) ? 1f : 0f);
                v.y = (Held(Act.Forward) ? 1f : 0f) - (Held(Act.Back) ? 1f : 0f);
                Vector2 s = PadInput.LeftStick; if (s.sqrMagnitude > 0.04f) v += s;
            }
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
        public static Vector2 Look()
        {
            var m = Mouse.current; Vector2 v = m == null ? Vector2.zero : m.delta.ReadValue() * 0.12f * Bindings.MouseSens;
            return v + PadLook();
        }
        public static bool Sprint() { return Held(Act.Sprint) || PadInput.Held(PadAct.Sprint); }
        public static bool InteractDown() { return Down(Act.Interact) || PadNoMod(PadAct.Interact); }
        public static bool InventoryDown() { return Down(Act.Inventory) || PadNoMod(PadAct.Inventory); }
        public static bool SneakDown() { return Down(Act.Sneak) || PadInput.Down(PadAct.Sneak); }
        public static bool MountDown() { return Down(Act.Mount) || PadInput.Down(PadAct.Mount); }
        public static bool CustomizeDown() { return Down(Key.G); }
        public static bool ViewToggleDown() { return Down(Key.F5) || Down(Key.Backquote) || PadInput.Down(PadAct.ViewToggle); }
        public static bool SkillMenuDown() { return Down(Act.SkillMenu) || PadInput.Down(PadAct.SkillMenu); }
        public static bool ProfileDown() { return Down(Act.Profile); }
        public static bool BestiaryDown() { return Down(Act.Bestiary); }
        public static bool JournalDown() { return Down(Act.Journal) || PadInput.Down(PadAct.Journal); }
        public static bool DrinkDown() { return Down(Act.Drink) || PadInput.Down(PadAct.Drink); }
        public static bool MapDown() { return Down(Act.Map) || PadInput.Down(PadAct.Map); }
        public static bool DiagToggleDown() { return Down(Key.F3); }
        public static int NavDown()
        {
            if (Bindings.Capturing) return 0;
            var k = Keyboard.current;
            if (PadInput.Down(PadBtn.DUp)) return -1; if (PadInput.Down(PadBtn.DDown)) return 1;
            if (k == null) return 0;
            return (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) ? -1 : (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) ? 1 : 0;
        }
        public static bool ConfirmDown()
        {
            if (Bindings.Capturing) return false;
            if (PadInput.Down(PadBtn.South)) return true;
            var k = Keyboard.current; return k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
        }
        public static int DigitPressed() { var k = Keyboard.current; if (k == null) return -1; for (int i = 0; i < 9; i++) if (k[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) return i; return -1; }
        public static bool CancelDown() { if (Bindings.Capturing) return false; return Down(Key.Escape) || (PadInput.Down(PadBtn.East) && !PadInput.Mod()); }
        public static bool GuideDown() { return Down(Key.Y); }
        public static bool PauseDown() { return Down(Key.P) || PadInput.Down(PadAct.Pause); }
        public static bool AttackDown() { var m = Mouse.current; return (m != null && m.leftButton.wasPressedThisFrame) || Down(Act.Attack) || PadInput.Down(PadAct.Attack); }
        public static bool AttackHeld() { var m = Mouse.current; return (m != null && m.leftButton.isPressed) || Held(Act.Attack) || PadInput.Held(PadAct.Attack); }
        public static bool GuardHeld() { var m = Mouse.current; return (m != null && m.rightButton.isPressed) || PadInput.Held(PadAct.Guard); }
        public static bool DodgeDown() { var k = Keyboard.current; return Down(Act.Dodge) || (k != null && k.leftAltKey.wasPressedThisFrame) || PadNoMod(PadAct.Dodge); }
        public static bool JumpDown() { return Down(Act.Jump) || PadNoMod(PadAct.Jump); }
        public static bool JumpHeld() { return Held(Act.Jump) || PadInput.Held(PadAct.Jump); }
        public static bool DiveHeld() { return Held(Act.Dodge) || PadInput.Held(PadAct.Dodge); }
        public static bool DevMenuDown() { return Down(Key.F1); }
        /// <summary>Hotkey cheat: 0=F2 mode dewa, 1=F4 ganti job, 2=F6 ganti evolusi, 3=F7 stamina/mana tak habis, 4=F8 tanpa cooldown, 5=Insert isi penuh.</summary>
        public static bool CheatDown(int i) { Key[] k = { Key.F2, Key.F4, Key.F6, Key.F7, Key.F8, Key.Insert }; return Down(k[i]); }
        public static bool DevUpHeld() { var k = Keyboard.current; return k != null && k.spaceKey.isPressed; }
        public static bool DevDownHeld() { var k = Keyboard.current; return k != null && k.leftCtrlKey.isPressed; }
        public static bool NumberDown(int i) { return Down(DigitKeys[i]); }     // quickbar item 1-6
        public static bool SkillKeyDown(int i) { return Down(SkillActs[i]) || PadSkill(i); }
        public static bool DebugDown(int n) { return n == 9 ? Down(Key.F9) : n == 10 ? Down(Key.F10) : n == 11 ? Down(Key.F11) : Down(Key.F12); }
#else
        static readonly Act[] SkillActs = { Act.Skill1, Act.Skill2, Act.Skill3, Act.Skill4, Act.Skill5, Act.Skill6, Act.Skill7 };
        static bool Down(Act a) { return Input.GetKeyDown(Bindings.Get(a)); }
        static bool Held(Act a) { return Input.GetKey(Bindings.Get(a)); }
        public static bool HasPad { get { return PadInput.Connected; } }

        public static Vector2 Move()
        {
            if (Bindings.Capturing) return Vector2.zero;
            var v = new Vector2((Held(Act.Right) ? 1f : 0f) - (Held(Act.Left) ? 1f : 0f), (Held(Act.Forward) ? 1f : 0f) - (Held(Act.Back) ? 1f : 0f));
            Vector2 s = PadInput.LeftStick; if (s.sqrMagnitude > 0.04f) v += s;
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
        public static Vector2 Look() { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f * Bindings.MouseSens + PadLook(); }
        public static bool Sprint() { return Held(Act.Sprint) || PadInput.Held(PadAct.Sprint); }
        public static bool InteractDown() { return Down(Act.Interact) || PadNoMod(PadAct.Interact); }
        public static bool InventoryDown() { return Down(Act.Inventory) || PadNoMod(PadAct.Inventory); }
        public static bool MountDown() { return Down(Act.Mount) || PadInput.Down(PadAct.Mount); }
        public static bool CustomizeDown() { return Input.GetKeyDown(KeyCode.G); }
        public static bool ViewToggleDown() { return Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.BackQuote) || PadInput.Down(PadAct.ViewToggle); }
        public static bool SkillMenuDown() { return Down(Act.SkillMenu) || PadInput.Down(PadAct.SkillMenu); }
        public static bool ProfileDown() { return Down(Act.Profile); }
        public static bool BestiaryDown() { return Down(Act.Bestiary); }
        public static bool JournalDown() { return Down(Act.Journal) || PadInput.Down(PadAct.Journal); }
        public static bool DrinkDown() { return Down(Act.Drink) || PadInput.Down(PadAct.Drink); }
        public static bool MapDown() { return Down(Act.Map) || PadInput.Down(PadAct.Map); }
        public static int NavDown() { if (Bindings.Capturing) return 0; if (PadInput.Down(PadBtn.DUp)) return -1; if (PadInput.Down(PadBtn.DDown)) return 1; return (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) ? -1 : (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) ? 1 : 0; }
        public static bool ConfirmDown() { if (Bindings.Capturing) return false; return PadInput.Down(PadBtn.South) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space); }
        public static int DigitPressed() { for (int i = 0; i < 9; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) return i; return -1; }
        public static bool CancelDown() { if (Bindings.Capturing) return false; return Input.GetKeyDown(KeyCode.Escape) || (PadInput.Down(PadBtn.East) && !PadInput.Mod()); }
        public static bool GuideDown() { return Input.GetKeyDown(KeyCode.Y); }
        public static bool PauseDown() { return Input.GetKeyDown(KeyCode.P) || PadInput.Down(PadAct.Pause); }
        public static bool AttackDown() { return Input.GetMouseButtonDown(0) || Down(Act.Attack) || PadInput.Down(PadAct.Attack); }
        public static bool AttackHeld() { return Input.GetMouseButton(0) || Held(Act.Attack) || PadInput.Held(PadAct.Attack); }
        public static bool GuardHeld() { return Input.GetMouseButton(1) || PadInput.Held(PadAct.Guard); }
        public static bool DodgeDown() { return Down(Act.Dodge) || Input.GetKeyDown(KeyCode.LeftAlt) || PadNoMod(PadAct.Dodge); }
        public static bool JumpDown() { return Down(Act.Jump) || PadNoMod(PadAct.Jump); }
        public static bool JumpHeld() { return Held(Act.Jump) || PadInput.Held(PadAct.Jump); }
        public static bool DiveHeld() { return Held(Act.Dodge) || PadInput.Held(PadAct.Dodge); }
        public static bool DevMenuDown() { return Input.GetKeyDown(KeyCode.F1); }
        public static bool CheatDown(int i) { KeyCode[] k = { KeyCode.F2, KeyCode.F4, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.Insert }; return Input.GetKeyDown(k[i]); }
        public static bool DevUpHeld() { return Input.GetKey(KeyCode.Space); }
        public static bool DevDownHeld() { return Input.GetKey(KeyCode.LeftControl); }
        public static bool NumberDown(int i) { return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)); }
        public static bool SkillKeyDown(int i) { return Down(SkillActs[i]) || PadSkill(i); }
        public static bool DebugDown(int n) { return Input.GetKeyDown(n == 9 ? KeyCode.F9 : n == 10 ? KeyCode.F10 : n == 11 ? KeyCode.F11 : KeyCode.F12); }
#endif
        // ---------- gamepad bersama (kedua mode input) ----------
        static bool PadNoMod(PadAct a) { return !PadInput.Mod() && PadInput.Down(a); }
        static readonly PadBtn[] SkillFace = { PadBtn.South, PadBtn.East, PadBtn.West, PadBtn.North };
        /// <summary>Skill 0-6: pengubah A + A/B/X/Y = skill 1-4, pengubah B + A/B/X = skill 5-7.</summary>
        static bool PadSkill(int i)
        {
            if (i < 4) return PadInput.Held(PadAct.ModA) && PadInput.Down(SkillFace[i]);
            if (i < 7) return PadInput.Held(PadAct.ModB) && PadInput.Down(SkillFace[i - 4]);
            return false;
        }
        static Vector2 PadLook()
        {
            Vector2 s = PadInput.RightStick;
            if (s.sqrMagnitude < 0.05f) return Vector2.zero;
            Vector2 v = s * (Time.unscaledDeltaTime * 170f * 0.12f * 8f) * Bindings.PadSens;
            if (Bindings.InvertY) v.y = -v.y;
            return v;
        }
        /// <summary>true bila pemain baru menekan serangan/interaksi/menu (dipakai HUD redup).</summary>
        public static bool AnyActivity() { return AttackDown() || InteractDown() || InventoryDown() || SkillMenuDown() || DrinkDown(); }
    }
}
