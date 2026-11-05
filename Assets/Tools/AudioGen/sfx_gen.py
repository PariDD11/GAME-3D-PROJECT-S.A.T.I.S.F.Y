# -*- coding: utf-8 -*-
"""Resep efek suara (SFX). render_all(outdir) menulis sfx_<nama>.wav (mono 44,1 kHz 16-bit)."""
import os, wave
import numpy as np
from synth import *
import inst as I


def L(layers, total=None):
    end = max(int(round(t0 * SR)) + len(x) for x, t0, g in layers)
    if total:
        end = max(end, NS(total))
    out = np.zeros(end)
    for x, t0, g in layers:
        i = int(round(t0 * SR)); out[i:i + len(x)] += x * g
    return out


def thump(f0=160, f1=55, d=0.15, tau=0.05):
    n = NS(d)
    return sweep(f0, f1, d) * env_exp(n, tau, 0.001)


def crack(d=0.03, lo=1500):
    n = NS(d)
    return hp(nz(d), lo) * env_exp(n, d / 4, 0.0004)


def body(d=0.12, cut=900, tau=0.03):
    n = NS(d)
    return lp(nz(d), cut) * env_exp(n, tau, 0.001)


def rev(x, rt=0.9, wet=0.25, damp=4000, seed=3):
    w = reverb_mono(np.concatenate([x, np.zeros(NS(rt))]), rt, damp, seed)
    w = w[:, 0] * 0.5 + w[:, 1] * 0.5
    m = np.max(np.abs(x)) + 1e-9
    base = np.concatenate([x, np.zeros(len(w) - len(x))])
    out = base + wet * w * (m / (np.max(np.abs(w)) + 1e-9)) * 0.6
    return out


def note(inst, f, d=0.3, vel=0.8, **k):
    return inst(f, d, vel, **k)


def N(m):
    return float(midi(m))


# ------------------------------------------------------------------ tempur
def s_swing():
    d = 0.22; n = NS(d); sh = env_bell(n, 0.45)
    return sweep_noise(500, 2600, d, shape=sh) * 0.85 + lp(nz(d), 420) * sh * 0.3


def s_swing2():
    d = 0.2; n = NS(d); sh = env_bell(n, 0.4)
    return sweep_noise(2900, 700, d, shape=sh) * 0.85 + lp(nz(d), 380) * sh * 0.3


def s_heavy_swing():
    d = 0.42; n = NS(d); sh = env_bell(n, 0.55)
    return L([(sweep_noise(240, 1900, d, bands=9, shape=sh), 0, 0.9), (lp(nz(d), 260) * sh, 0, 0.5), (sweep(110, 62, d) * sh, 0, 0.3)])


def s_hit():
    return L([(thump(170, 60, 0.14, 0.04), 0, 1.0), (crack(0.03, 1800), 0, 0.7), (body(0.12, 900, 0.03), 0, 0.5)])


def s_hit_heavy():
    return rev(L([(thump(130, 40, 0.3, 0.09), 0, 1.0), (crack(0.045, 1200), 0, 0.85), (body(0.22, 650, 0.06), 0, 0.6),
                  (partials(700, [1, 2.76, 5.4], [0.3, 0.15, 0.08], [0.25, 0.15, 0.1], 0.4), 0, 0.3)]), 0.5, 0.18)


def s_hit_light():
    return L([(partials(900, [1, 2.3, 4.1], [1, 0.4, 0.2], [0.05, 0.03, 0.02], 0.16), 0, 1.0), (crack(0.012, 3000), 0, 0.5), (thump(300, 150, 0.07, 0.02), 0, 0.4)])


def s_bow_shot():
    tw = partials(190, [1, 2, 3, 4, 5, 6], [1, 0.8, 0.6, 0.4, 0.3, 0.2], [0.35, 0.3, 0.25, 0.2, 0.15, 0.12], 0.5, a=0.001)
    sh = env_bell(NS(0.25), 0.3)
    return L([(tw, 0, 0.8), (sweep_noise(3200, 800, 0.25, shape=sh), 0.01, 0.35), (thump(140, 70, 0.08, 0.03), 0, 0.5)])


def s_arrow_hit():
    q = partials(420, [1, 2.1], [0.4, 0.2], [0.12, 0.08], 0.32)
    q = q * (1 + 0.4 * np.sin(2 * np.pi * 30 * np.arange(len(q)) / SR))
    return L([(thump(260, 90, 0.1, 0.03), 0, 1.0), (crack(0.02, 1500), 0, 0.5), (q, 0.015, 0.3)])


def s_block():
    cl = partials(880, [1, 2.32, 4.25, 6.63, 9.38], [1, 0.7, 0.5, 0.35, 0.25], [0.45, 0.3, 0.22, 0.15, 0.1], 0.8, a=0.0008)
    return rev(L([(cl, 0, 0.8), (crack(0.015, 2500), 0, 0.6), (thump(200, 100, 0.08, 0.03), 0, 0.5)]), 0.6, 0.2)


def s_dodge():
    d = 0.3; n = NS(d)
    cloth = sweep_noise(900, 2200, d, shape=env_bell(n, 0.4)) * 0.5 + lp(nz(d), 700) * env_bell(n, 0.5) * 0.2
    return L([(cloth, 0, 1.0), (thump(120, 70, 0.1, 0.04), 0.22, 0.4)])


def s_perfect_dodge():
    d = 0.35; n = NS(d)
    w = sweep_noise(1200, 5200, d, shape=env_bell(n, 0.6)) * 0.5
    sh = L([(partials(2600, [1, 1.5, 2.01], [0.5, 0.3, 0.2], [0.3, 0.25, 0.2], 0.55), 0, 1.0), (partials(3900, [1, 1.5], [0.35, 0.2], [0.25, 0.2], 0.5), 0, 1.0)])
    return rev(L([(w, 0, 1.0), (sh, 0.1, 0.55)]), 0.7, 0.2)


def s_break():
    deb = sweep_noise(3000, 300, 0.5, shape=env_exp(NS(0.5), 0.18, 0.003))
    return rev(L([(thump(100, 35, 0.5, 0.15), 0, 1.0), (crack(0.06, 900), 0, 0.9), (deb, 0.01, 0.6),
                  (partials(500, [1, 2.76, 5.4, 8.9], [0.5, 0.3, 0.2, 0.1], [0.5, 0.4, 0.3, 0.2], 0.7), 0, 0.3)]), 0.8, 0.2)


def s_hurt():
    v = vocal(140, 0.28, 'uh', f1=105, rough=0.03, r=0.1, a=0.01, breath=0.05)
    return L([(v, 0, 0.9), (body(0.1, 1500, 0.03), 0, 0.2)])


def s_faint():
    v = vocal(150, 0.8, 'uh', f1=70, vib=0.01, r=0.4, a=0.03, breath=0.05)
    return L([(v, 0, 0.9), (thump(90, 45, 0.25, 0.1), 0.75, 0.9), (body(0.2, 500, 0.08), 0.75, 0.5)])


def s_status_hit():
    d = 0.5; n = NS(d); t = np.arange(n) / SR
    siz = hp(nz(d), 3000) * (0.5 + 0.5 * np.sin(2 * np.pi * 37 * t)) * env_exp(n, 0.25, 0.005)
    return L([(siz, 0, 0.5), (sweep(900, 200, 0.4) * env_exp(NS(0.4), 0.15, 0.003), 0, 0.35), (sweep(600, 300, 0.1) * env_exp(NS(0.1), 0.04), 0.05, 0.25)])


def s_skill_cast():
    ls = []
    for i in range(6):
        ls.append((sweep(800 * 1.25 ** i, 1800 * 1.25 ** i, 0.5) * env_bell(NS(0.5), 0.5) * 0.3, 0.015 * i, 0.5))
    sh = sweep_noise(1500, 6000, 0.5, shape=env_bell(NS(0.5), 0.7))
    ls += [(sh, 0, 0.35), (partials(1568, [1, 2.76, 5.4], [0.5, 0.3, 0.1], [0.5, 0.3, 0.2], 0.65), 0.35, 0.5)]
    return rev(L(ls), 0.7, 0.2)


def s_heal():
    ls = [(I.celesta(N(m), 0.3, 0.8, ring=1.2), 0.09 * i, 0.7) for i, m in enumerate([72, 76, 79, 84])]
    ls.append((I.strings(N(72), 0.7, 0.8, a=0.2, r=0.5, cut=1800), 0.0, 0.25))
    return rev(L(ls), 1.0, 0.25)


def s_potion():
    ls = []
    for t0, f in [(0.0, 300), (0.13, 380), (0.27, 330)]:
        ls.append((sweep(f, f * 2, 0.07) * env_exp(NS(0.07), 0.03, 0.002), t0, 0.5))
    t = np.arange(NS(0.5)) / SR
    ls.append((bp(nz(0.5), 400, 1500) * (0.5 + 0.5 * np.sin(2 * np.pi * 14 * t)) * env_bell(len(t), 0.4), 0, 0.3))
    ls.append((sweep(220, 110, 0.12) * env_exp(NS(0.12), 0.05), 0.5, 0.7))
    return L(ls)


def s_monster_growl():
    v = vocal(75, 0.85, 'uh', f1=62, rough=0.06, r=0.28, a=0.08, breath=0.08)
    n = len(v)
    return L([(v, 0, 1.0), (lp(nz(n / SR), 500) * env_bell(n, 0.4), 0, 0.25)])


def s_monster_attack():
    v = vocal(110, 0.22, 'ah', f1=90, rough=0.08, r=0.08, a=0.005, breath=0.06)
    return L([(crack(0.04, 800), 0, 0.8), (thump(150, 60, 0.15, 0.05), 0, 0.8), (v, 0, 0.55),
              (sweep(500, 150, 0.1) * env_exp(NS(0.1), 0.04), 0.01, 0.3)])


def s_monster_hurt():
    return L([(vocal(300, 0.35, 'ah', f1=180, rough=0.05, r=0.2, a=0.005, breath=0.06), 0, 0.9), (body(0.08, 2000, 0.02), 0, 0.2)])


def s_monster_die():
    v = vocal(160, 1.1, 'o', f1=45, rough=0.07, vib=0.01, r=0.5, a=0.02, breath=0.06)
    return L([(v, 0, 1.0), (body(1.1, 400, 0.4), 0, 0.3), (thump(80, 40, 0.3, 0.12), 1.0, 0.8)])


def s_boss_roar():
    n = NS(2.0)
    r1 = vocal(70, 2.0, 'ah', f1=48, rough=0.07, r=0.8, a=0.15, breath=0.07)
    r2 = vocal(140, 1.8, 'o', f1=90, rough=0.06, r=0.7, a=0.2, breath=0.05)
    nzz = sweep_noise(300, 1500, 1.8, shape=env_bell(NS(1.8), 0.35))
    sb = sweep(60, 35, 2.0) * env_bell(n, 0.4)
    return rev(L([(r1, 0, 1.0), (r2, 0, 0.6), (nzz, 0, 0.35), (sb, 0, 0.4)]), 1.8, 0.28)


# ------------------------------------------------------------------ sting / quest
def s_victory():
    ls = []
    for t0, ch, d in [(0.0, [60, 64, 67], 0.45), (0.5, [65, 69, 72], 0.45), (1.0, [67, 71, 74], 0.4), (1.5, [72, 76, 79, 84], 1.6)]:
        for m in ch:
            ls.append((I.horn(N(m), d, 0.7, a=0.04, r=0.4), t0, 0.5))
            ls.append((I.strings(N(m + 12), d, 0.5, a=0.05, r=0.5), t0, 0.25))
    ls += [(I.taiko(0.9, 70), 0.0, 0.8), (I.taiko(0.9, 70), 1.5, 0.9), (I.chime(N(96), 0.5, 0.6, ring=2.0), 1.5, 0.4), (I.cymbal(2.2, 0.5), 1.5, 0.35)]
    return rev(L(ls), 1.6, 0.25)


def s_defeat():
    ls = []
    for t0, ch, d in [(0.0, [57, 60, 64], 1.0), (1.2, [55, 58, 62], 1.0), (2.4, [53, 56, 60], 1.6)]:
        for m in ch:
            ls.append((I.strings(N(m), d, 0.7, a=0.3, r=0.8, cut=1400), t0, 0.5))
    ls += [(I.bell_toll(N(45), 0.8, 5.0), 0.0, 0.5), (I.taiko(0.8, 55), 0.0, 0.7), (I.taiko(0.7, 50), 2.4, 0.7)]
    return rev(L(ls), 2.0, 0.3)


def _cel(ms, step, vel=0.8, ring=1.4, t0=0.0):
    return [(I.celesta(N(m), 0.3, vel, ring=ring), t0 + step * i, 0.7) for i, m in enumerate(ms)]


def s_quest_accept():
    return rev(L(_cel([76, 83], 0.12, ring=1.2) + [(I.strings(N(64), 0.5, 0.6, a=0.1, r=0.4), 0, 0.2)]), 0.8, 0.2)


def s_quest_progress():
    return rev(L(_cel([81], 0, ring=1.0)), 0.6, 0.15)


def s_quest_done():
    return rev(L(_cel([79, 83, 86, 91], 0.1, ring=1.4)), 0.9, 0.22)


def coin_tink(f=3100, ring=0.3):
    return L([(partials(f, [1, 1.51, 2.2], [1, 0.6, 0.3], [ring, ring * 0.6, ring * 0.4], 0.5, a=0.0005), 0, 1.0), (crack(0.012, 4000), 0, 0.4)])


def s_quest_reward():
    ls = [(coin_tink(3000 + 300 * i), 0.07 * i, 0.5) for i in range(3)] + _cel([72, 76, 79, 84, 88], 0.08, ring=1.5, t0=0.2)
    return rev(L(ls), 1.0, 0.22)


def s_quest_fail():
    ls = [(I.pluck(N(52), 0.3, 0.9, decay=0.9, bright=0.35), 0.0, 0.8), (I.pluck(N(48), 0.3, 0.9, decay=1.1, bright=0.3), 0.3, 0.8),
          (I.bell_toll(N(48), 0.6, 2.5), 0.3, 0.4)]
    return rev(L(ls), 1.0, 0.3)


def s_quest():
    ls = [(I.choir(N(m), 0.8, 0.7, vowel='a', a=0.25, r=0.7, voices=3), 0, 0.5) for m in (60, 67, 76)] + [(I.chime(N(88), 0.4, 0.6, ring=2.0), 0.35, 0.4)]
    return rev(L(ls), 1.6, 0.3)


def s_rankup():
    ls = []
    for t0, ch, d in [(0.0, [60, 67], 0.3), (0.35, [65, 72], 0.3), (0.7, [67, 74], 0.3), (1.05, [72, 76, 79, 84], 1.4)]:
        for m in ch:
            ls.append((I.horn(N(m), d, 0.75, a=0.03, r=0.35), t0, 0.5))
            ls.append((I.strings(N(m + 12), d, 0.5, a=0.04, r=0.5), t0, 0.22))
    ls += [(I.taiko(0.9, 72), 1.05, 0.7)] + _cel([96, 100, 103, 108], 0.07, ring=2.0, t0=1.1) + [(I.cymbal(2.2, 0.5), 1.05, 0.3)]
    return rev(L(ls), 1.5, 0.25)


def s_levelup():
    ls = _cel([72, 74, 76, 79, 84, 88], 0.07, vel=0.8, ring=1.5)
    ls += [(sweep_noise(1500, 7000, 0.7, shape=env_bell(NS(0.7), 0.6)), 0.05, 0.16), (I.strings(N(72), 1.0, 0.6, a=0.2, r=0.6, cut=2000), 0, 0.22)]
    return rev(L(ls), 1.2, 0.22)


def s_track_found():
    ls = _cel([74, 81], 0.14, ring=1.0) + [(sweep_noise(300, 900, 0.4, shape=env_bell(NS(0.4), 0.5)), 0, 0.18)]
    return rev(L(ls), 0.8, 0.2)


def s_claim():
    ls = [(coin_tink(float(R().uniform(2800, 4200)), 0.25), float(R().uniform(0, 0.45)), 0.4) for _ in range(7)] + _cel([79, 84, 91], 0.1, ring=1.4, t0=0.4)
    return rev(L(ls), 0.9, 0.2)


def s_pickup():
    return L([(sweep(500, 900, 0.06) * env_exp(NS(0.06), 0.03, 0.002), 0, 0.8), (bp(nz(0.05), 1500, 5000) * env_exp(NS(0.05), 0.015), 0, 0.3)], 0.15)


def s_gather():
    ls = []
    for i in range(7):
        t0 = float(R().uniform(0, 0.3)); d = float(R().uniform(0.04, 0.1))
        ls.append((bp(nz(d), 1500, 6500) * env_exp(NS(d), d / 3, 0.002), t0, float(R().uniform(0.4, 1.0))))
    ls.append((bp(nz(0.05), 3000, 7500) * env_exp(NS(0.05), 0.015), 0.34, 0.5))
    return L(ls, 0.45)


def s_coin():
    return rev(L([(coin_tink(3100, 0.3), 0, 0.8), (coin_tink(4300, 0.25), 0.07, 0.6)]), 0.4, 0.12)


def s_paper():
    ls = [(bp(nz(0.18), 2500, 7000) * env_bell(NS(0.18), 0.3), 0, 0.7)]
    for i in range(3):
        d = float(R().uniform(0.05, 0.09))
        ls.append((bp(nz(d), 3000, 8000) * env_exp(NS(d), d / 3, 0.002), 0.2 + 0.09 * i, 0.5))
    return L(ls, 0.5)


def creak(d, f0, f1, wob=3.0, wd=25.0):
    n = NS(d); t = np.arange(n) / SR
    f = np.linspace(f0, f1, n) + wd * np.sin(2 * np.pi * wob * t) + 8 * R().standard_normal(n) * 0.2
    ph = 2 * np.pi * np.cumsum(f) / SR
    out = np.zeros(n)
    for k in range(1, 18):
        g = (1.0 / k) * formant_gain(k * f, [(700, 200, 1.0), (1500, 300, 0.6)])
        out += np.where(k * f < SR * 0.45, g, 0) * np.sin(k * ph)
    return out * env_bell(n, 0.5) ** 0.6 / (np.max(np.abs(out)) + 1e-9)


def s_gate():
    ls = [(creak(1.2, 70, 120), 0, 0.8), (lp(nz(1.3), 160) * env_bell(NS(1.3), 0.5), 0, 0.5)]
    for _ in range(14):
        t0 = float(R().uniform(0.0, 1.2)); ls.append((hp(nz(0.12), 2500) * env_exp(NS(0.12), 0.03), t0, float(R().uniform(0.1, 0.4))))
    ls += [(thump(80, 40, 0.3, 0.1), 1.35, 1.0), (body(0.25, 400, 0.08), 1.35, 0.5)]
    return rev(L(ls), 1.0, 0.2)


def s_door():
    ls = [(creak(0.5, 140, 220, 4.0, 15.0), 0, 0.6), (crack(0.015, 2200), 0.55, 0.7), (partials(1200, [1, 2.5], [0.3, 0.2], [0.03, 0.02], 0.1), 0.55, 0.4),
          (thump(110, 55, 0.12, 0.04), 0.62, 0.7), (body(0.1, 600, 0.03), 0.62, 0.4)]
    return rev(L(ls), 0.5, 0.15)


def s_tower_portal():
    d = 2.2; n = NS(d); t = np.arange(n) / SR
    hum = sum(I.warm(110, d - 1.0, 0.8, a=0.7, r=1.0, cut=900) for _ in (0,))
    hum = fit(hum, n)
    sh = sum(sweep(600 * 1.5 ** i, 2400 * 1.5 ** i * 0.6, d, 'lin') * (0.6 + 0.4 * np.sin(2 * np.pi * (3 + i) * t)) * 0.08 for i in range(3)) * env_bell(n, 0.55)
    return rev(L([(hum, 0, 0.8), (sweep(100, 400, d, 'lin') * env_bell(n, 0.7) * 0.15, 0, 1.0), (sh, 0, 1.0),
                  (sweep_noise(500, 4000, d, shape=env_bell(n, 0.6)), 0, 0.22)]), 1.4, 0.3)


def s_tower_floor_clear():
    ls = _cel([72, 79, 88], 0.1, ring=1.8) + [(I.strings(N(m), 1.4, 0.6, a=0.3, r=0.6, cut=1600), 0.25, 0.3) for m in (48, 55, 64)]
    return rev(L(ls), 1.4, 0.25)


def s_chest_open():
    ls = [(creak(0.45, 200, 320, 5.0, 20.0), 0, 0.55), (crack(0.02, 2000), 0.5, 0.6)]
    for i in range(6):
        ls.append((I.celesta(float(R().uniform(1200, 4500)), 0.1, 0.6, ring=0.5), 0.55 + 0.06 * i, 0.4))
    return rev(L(ls), 0.9, 0.22)


def s_thunder():
    n = NS(3.4); t = np.arange(n) / SR
    rum = lp(nz(3.4), 170) * (np.minimum(t / 0.12, 1) * np.exp(-t / 1.3)) * (1 + 0.5 * np.sin(2 * np.pi * 7 * t + 1))
    mid = lp(nz(3.4), 700) * np.exp(-t / 0.7) * 0.5
    return rev(L([(crack(0.2, 1500) * 1.0, 0, 1.0), (rum, 0.02, 1.4), (mid, 0.02, 0.6)]), 2.2, 0.3)


def s_bell_toll():
    """Lonceng besar Katedral: satu pukulan, dengung panjang (nada dasar ~140 Hz), gema luas. Dipukul enam kali oleh CathedralBell."""
    ls = [(I.bell_toll(140.0, 0.95, 7.5), 0, 0.9), (I.bell_toll(280.0 * 1.004, 0.5, 5.5), 0, 0.3), (I.bell_toll(70.0, 0.6, 8.0), 0, 0.35),
          (hp(nz(0.06), 2200) * env_exp(NS(0.06), 0.012), 0, 0.2)]
    return rev(L(ls), 3.0, 0.3)


RECIPES = {
    # nama: (fungsi, puncak)
    'swing': (s_swing, 0.55), 'swing2': (s_swing2, 0.55), 'heavy_swing': (s_heavy_swing, 0.7), 'hit': (s_hit, 0.85), 'hit_heavy': (s_hit_heavy, 0.95),
    'hit_light': (s_hit_light, 0.6), 'bow_shot': (s_bow_shot, 0.7), 'arrow_hit': (s_arrow_hit, 0.75), 'block': (s_block, 0.8), 'dodge': (s_dodge, 0.5),
    'perfect_dodge': (s_perfect_dodge, 0.6), 'break': (s_break, 0.95), 'hurt': (s_hurt, 0.8), 'faint': (s_faint, 0.8), 'status_hit': (s_status_hit, 0.6),
    'skill_cast': (s_skill_cast, 0.65), 'heal': (s_heal, 0.6), 'potion': (s_potion, 0.55), 'monster_growl': (s_monster_growl, 0.8),
    'monster_attack': (s_monster_attack, 0.85), 'monster_hurt': (s_monster_hurt, 0.75), 'monster_die': (s_monster_die, 0.85), 'boss_roar': (s_boss_roar, 0.95),
    'victory': (s_victory, 0.8), 'defeat': (s_defeat, 0.75), 'quest_accept': (s_quest_accept, 0.6), 'quest_progress': (s_quest_progress, 0.5),
    'quest_done': (s_quest_done, 0.65), 'quest_reward': (s_quest_reward, 0.65), 'quest_fail': (s_quest_fail, 0.65), 'quest': (s_quest, 0.55),
    'rankup': (s_rankup, 0.85), 'levelup': (s_levelup, 0.7), 'track_found': (s_track_found, 0.5), 'claim': (s_claim, 0.65), 'pickup': (s_pickup, 0.5),
    'gather': (s_gather, 0.5), 'coin': (s_coin, 0.55), 'paper': (s_paper, 0.45), 'gate': (s_gate, 0.8), 'door': (s_door, 0.7),
    'tower_portal': (s_tower_portal, 0.7), 'tower_floor_clear': (s_tower_floor_clear, 0.65), 'chest_open': (s_chest_open, 0.65), 'thunder': (s_thunder, 0.9),
    'bell_toll': (s_bell_toll, 0.8),   # n89: lonceng Katedral (ditambahkan paling akhir supaya seed efek lain tidak bergeser)
}


def finish(x, peak):
    x = np.asarray(x, dtype=float)
    x = x - np.mean(x)
    n = len(x)
    fi = min(NS(0.002), n // 4); fo = min(NS(0.012), n // 4)
    x[:fi] *= np.linspace(0, 1, fi); x[-fo:] *= np.linspace(1, 0, fo)
    # buang keheningan di ekor
    thr = 0.012 * (np.max(np.abs(x)) + 1e-9)
    idx = np.where(np.abs(x) > thr)[0]
    if len(idx):
        x = x[:min(n, idx[-1] + NS(0.06))]
        fo = min(NS(0.05), len(x) // 4); x[-fo:] *= np.linspace(1, 0, fo)
    return norm(x, peak)


def write_wav(path, x):
    pcm = (np.clip(x, -1, 1) * 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())


def render_all(outdir, only=None):
    os.makedirs(outdir, exist_ok=True)
    res = {}
    for i, (name, (fn, pk)) in enumerate(RECIPES.items()):
        if only and name not in only:
            continue
        reseed(1000 + i)
        x = finish(fn(), pk)
        write_wav(os.path.join(outdir, 'sfx_' + name + '.wav'), x)
        res[name] = (len(x) / SR, float(np.max(np.abs(x))), float(np.sqrt(np.mean(x ** 2))))
    return res


if __name__ == '__main__':
    import sys, time
    t0 = time.time()
    r = render_all(sys.argv[1] if len(sys.argv) > 1 else 'out_sfx')
    for k, (d, p, rm) in r.items():
        print(f"{k:18s} {d:5.2f}s peak={p:.2f} rms={20*np.log10(rm+1e-9):6.1f}dB")
    print(len(r), 'sfx,', round(time.time() - t0, 1), 's')
