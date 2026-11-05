# -*- coding: utf-8 -*-
"""Instrumen sintetis: tiap fungsi mengembalikan array mono. f=Hz, d=lama nada (detik), vel=0..1."""
import numpy as np
from synth import *


def _fin(x, vel, peak=1.0):
    x = np.array(x, dtype=float)
    k = min(NS(0.12), len(x) // 4)
    if k > 2:
        x[-k:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, k))
    m = float(np.max(np.abs(x))) + 1e-9
    return x * (vel * peak / m)


def _sawlp(cut):
    return lambda k, fk, t: (1.0 / k) / np.sqrt(1 + (fk / cut) ** 4)


def _hn(f, cut, cap=34):
    return int(max(4, min(cap, cut * 2.6 / f)))


# ------------------------------------------------------------ sustain
def strings(f, d, vel=0.8, a=0.4, r=0.6, cut=2400, voices=3, vib=0.003, trem=0.0):
    n = int(round((d + r) * SR))
    dets = np.linspace(-10, 10, voices) if voices > 1 else [0.0]
    out = sum(addv(f, n, _sawlp(cut), H=_hn(f, cut), vib_depth=vib, vib_delay=0.4, detune=float(dd)) for dd in dets)
    e = fit(env_sus(d, a, r), n)
    if trem:
        e = e * (1 - 0.35 * (0.5 + 0.5 * np.sin(2 * np.pi * trem * np.arange(n) / SR)))
    return _fin(out * e, vel)


def warm(f, d, vel=0.8, a=1.2, r=2.0, cut=1300):
    return strings(f, d, vel, a=a, r=r, cut=cut, voices=4, vib=0.002)


def choir(f, d, vel=0.8, vowel='a', a=0.9, r=1.3, voices=4):
    n = int(round((d + r) * SR))
    forms = VOWELS[vowel]
    out = np.zeros(n)
    for dd in np.linspace(-14, 14, voices):
        out += addv(f, n, lambda k, fk, t: (1.0 / k ** 0.9) * formant_gain(fk, forms), H=min(34, int(3800 / f) + 3),
                    vib_depth=0.0045, vib_delay=0.5, vib_rate=5.2 + 0.2 * float(dd) / 14, detune=float(dd))
    out += 0.02 * np.max(np.abs(out)) * bp(nz(n / SR), 600, 3500)
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def horn(f, d, vel=0.8, a=0.09, r=0.35, hi=3400):
    n = int(round((d + r) * SR)); t = np.arange(n) / SR
    cut = 550 + hi * (1 - np.exp(-t / 0.20))
    out = sum(addv(f, n, lambda k, fk, tt_: (1.0 / k) / np.sqrt(1 + (fk / cut) ** 4), H=_hn(f, hi, 30),
                   vib_depth=0.004, vib_delay=0.35, detune=dd) for dd in (-6.0, 6.0))
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def flute(f, d, vel=0.8, a=0.06, r=0.18, breath=0.06):
    n = int(round((d + r) * SR))
    amps = [1.0, 0.28, 0.08, 0.03, 0.012]
    out = addv(f, n, lambda k, fk, t: amps[k - 1], H=5, vib_depth=0.006, vib_delay=0.25, vib_rate=5.0)
    e = fit(env_sus(d, a, r), n)
    out = out / (np.max(np.abs(out)) + 1e-9)
    out = out + breath * bp(nz(n / SR), 1500, 5500) * (0.4 + 0.6 * np.minimum(np.arange(n) / SR / 0.05, 1))
    return _fin(out * e, vel)


def reed(f, d, vel=0.8, a=0.12, r=0.25):
    n = int(round((d + r) * SR))
    forms = [(1100, 250, 1.0), (2300, 400, 0.5)]
    out = sum(addv(f, n, lambda k, fk, t: (1.0 / k) * (0.35 + formant_gain(fk, forms)) / np.sqrt(1 + (fk / 2600) ** 4),
                   H=_hn(f, 2600), vib_depth=0.003, vib_delay=0.3, detune=dd) for dd in (-7.0, 7.0))
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def cello(f, d, vel=0.8, a=0.07, r=0.3, cut=1000):
    n = int(round((d + r) * SR))
    out = addv(f, n, _sawlp(cut), H=_hn(f, cut), vib_depth=0.004, vib_delay=0.25)
    out += 0.03 * np.max(np.abs(out)) * bp(nz(n / SR), 300, 2000)
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def sub(f, d, vel=0.8, a=0.6, r=1.2, beat=0.0):
    n = int(round((d + r) * SR)); t = np.arange(n) / SR
    out = np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * 2 * f * t + 1.0)
    if beat:
        out = 0.5 * (np.sin(2 * np.pi * f * t) + np.sin(2 * np.pi * (f + beat) * t)) + 0.2 * np.sin(2 * np.pi * 2 * f * t)
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def organ(f, d, vel=0.8, a=0.05, r=0.3):
    n = int(round((d + r) * SR)); t = np.arange(n) / SR
    bars = {1: 1.0, 2: 0.8, 3: 0.55, 4: 0.5, 6: 0.3, 8: 0.22}
    out = np.zeros(n)
    for dd in (-5.0, 0.0, 6.0):
        fd = 2 ** (dd / 1200)
        for k, g in bars.items():
            if f * k * fd < SR * 0.45:
                out += g * np.sin(2 * np.pi * f * k * fd * t + R().uniform(0, 6.28))
    out *= 1 - 0.08 * np.sin(2 * np.pi * 5.8 * t)
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def glass(f, d, vel=0.8, a=0.7, r=1.8):
    n = int(round((d + r) * SR)); t = np.arange(n) / SR
    amps = [1.0, 0.45, 0.2, 0.1]
    out = addv(f, n, lambda k, fk, tt_: amps[k - 1], H=4)
    out *= 1 - 0.22 * (0.5 + 0.5 * np.sin(2 * np.pi * 0.23 * t))
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def bowed(f, d, vel=0.8, a=0.9, r=1.4, cut=1500):
    n = int(round((d + r) * SR)); t = np.arange(n) / SR
    out = addv(f, n, lambda k, fk, tt_: (1.0 / k ** 1.3) / np.sqrt(1 + (fk / cut) ** 4), H=_hn(f, cut, 22), vib_depth=0.0015, vib_delay=0.8)
    out += 0.05 * np.max(np.abs(out)) * bp(nz(n / SR), 2500, 7000)
    out *= 1 - 0.18 * (0.5 + 0.5 * np.sin(2 * np.pi * 6.3 * t))
    return _fin(out * fit(env_sus(d, a, r), n), vel)


def gliss(f0, f1, d, vel=0.8, a=0.5, r=0.8, cut=1800):
    """String yang meluncur (glissando) dari f0 ke f1."""
    n = int(round((d + r) * SR)); t = np.arange(n) / SR
    fm = f0 * (f1 / f0) ** np.clip(t / d, 0, 1)
    ph = 2 * np.pi * np.cumsum(fm) / SR
    out = np.zeros(n)
    for k in range(1, 18):
        g = (1.0 / k) / np.sqrt(1 + (k * fm / cut) ** 4)
        out += np.where(k * fm < SR * 0.45, g, 0) * np.sin(k * ph + R().uniform(0, 6.28))
    return _fin(out * fit(env_sus(d, a, r), n), vel)


# ------------------------------------------------------------ petik / lonceng
def pluck(f, d, vel=0.8, decay=1.4, bright=0.6, pick=0.25):
    dur = max(d, 0.1) + decay * 1.2
    n = int(round(dur * SR)); t = np.arange(n) / SR
    out = np.zeros(n)
    K = int(min(16, SR * 0.45 / f))
    for k in range(1, K + 1):
        a = (1.0 / k ** 1.15) * (bright ** (k - 1))
        tau = decay / (1 + 0.45 * (k - 1))
        out += a * np.exp(-t / tau) * np.sin(2 * np.pi * f * k * (1 + 0.00025 * k * k) * t + R().uniform(0, 6.28))
    m = NS(0.025)
    out[:m] += pick * 0.25 * bp(nz(0.025), 1800, 6500) * np.exp(-np.arange(m) / (0.005 * SR))
    out *= np.minimum(t / 0.002, 1.0)
    return _fin(out, vel)


def lute(f, d, vel=0.8):
    x = pluck(f, d, 1.0, decay=1.0, bright=0.52, pick=0.3)
    x = x + 0.25 * bp(x, 180, 520)
    return _fin(x, vel)


def harp(f, d, vel=0.8):
    return pluck(f, d, vel, decay=2.4, bright=0.42, pick=0.12)


def oud(f, d, vel=0.8):
    x = pluck(f, d, 1.0, decay=0.75, bright=0.72, pick=0.45)
    x = x + 0.4 * bp(x, 900, 1700)
    return _fin(x, vel)


def pizz(f, d, vel=0.8):
    return pluck(f, 0.1, vel, decay=0.35, bright=0.5, pick=0.35)


def celesta(f, d, vel=0.8, ring=1.8):
    x = partials(f, [1, 2.0, 4.0, 5.4], [1, 0.25, 0.12, 0.06], [ring, ring * 0.5, ring * 0.25, ring * 0.15], max(d, 0.2) + ring * 1.4, a=0.003)
    return _fin(x, vel)


def chime(f, d, vel=0.8, ring=2.2):
    x = partials(f, [1, 2.76, 5.4, 8.93], [1, 0.45, 0.22, 0.1], [ring, ring * 0.55, ring * 0.35, ring * 0.2], max(d, 0.2) + ring * 1.4, a=0.002)
    return _fin(x, vel)


def piano(f, d, vel=0.8, decay=3.2):
    dur = max(d, 0.2) + decay * 0.9
    n = int(round(dur * SR)); t = np.arange(n) / SR
    out = np.zeros(n)
    K = int(min(14, SR * 0.45 / f))
    base = decay * (440.0 / max(f, 60.0)) ** 0.35
    for k in range(1, K + 1):
        a = (1.0 / k ** 1.05) * (0.72 ** (k - 1))
        out += a * np.exp(-t / (base / (1 + 0.55 * (k - 1)))) * np.sin(2 * np.pi * f * k * np.sqrt(1 + 0.0004 * k * k) * t + R().uniform(0, 6.28))
    m = NS(0.03)
    out[:m] += 0.05 * bp(nz(0.03), 400, 3000) * np.exp(-np.arange(m) / (0.006 * SR))
    out *= np.minimum(t / 0.002, 1.0)
    return _fin(out, vel)


def bell_toll(f, vel=0.8, d=6.0):
    return _fin(partials(f, [0.5, 1.0, 1.19, 1.56, 2.0, 2.51, 3.0], [0.7, 1, 0.8, 0.5, 0.4, 0.3, 0.2], [6, 5, 4, 3, 2.2, 1.6, 1.2], d, a=0.003), vel)


def bamboo(vel=0.8, f=None):
    f = f or float(R().uniform(900, 1500))
    return _fin(partials(f, [1, 2.7, 4.6], [1, 0.3, 0.1], [0.04, 0.025, 0.015], 0.2, a=0.001), vel)


# ------------------------------------------------------------ perkusi
def kick(vel=0.9):
    n = NS(0.4)
    x = sweep(125, 42, 0.4) * env_exp(n, 0.11, 0.001) + 0.25 * hp(nz(0.4), 1500) * env_exp(n, 0.004, 0.0005)
    return _fin(x, vel)


def taiko(vel=0.9, f=66.0):
    n = NS(1.5)
    body = sweep(f * 2.1, f, 1.5) * env_exp(n, 0.32, 0.001)
    thump = lp(nz(1.5), 260) * env_exp(n, 0.05, 0.001) * 0.8
    skin = hp(nz(1.5), 2200) * env_exp(n, 0.008, 0.0005) * 0.25
    return _fin(body + thump + skin, vel)


def tom(vel=0.8, f=110.0):
    n = NS(0.8)
    x = sweep(f * 1.6, f, 0.8) * env_exp(n, 0.16, 0.001) + 0.3 * lp(nz(0.8), 700) * env_exp(n, 0.03, 0.001)
    return _fin(x, vel)


def framedrum(vel=0.8, f=165.0):
    n = NS(0.55)
    x = sweep(f * 1.4, f, 0.55) * env_exp(n, 0.09, 0.001) + 0.35 * bp(nz(0.55), 700, 3200) * env_exp(n, 0.02, 0.0005)
    return _fin(x, vel)


def snare(vel=0.8):
    n = NS(0.3)
    x = bp(nz(0.3), 1400, 7500) * env_exp(n, 0.07, 0.0005) + 0.55 * sweep(210, 170, 0.3) * env_exp(n, 0.04, 0.0005)
    return _fin(x, vel)


def hat(vel=0.6, op=False):
    d = 0.35 if op else 0.07
    n = NS(d)
    return _fin(hp(nz(d), 7000) * env_exp(n, d / 4, 0.0005), vel)


def shaker(vel=0.5):
    n = NS(0.12)
    t = np.arange(n) / SR
    return _fin(bp(nz(0.12), 3500, 9500) * np.sin(np.pi * np.clip(t / 0.12, 0, 1)) ** 2, vel)


def cymbal(d=3.0, vel=0.6):
    n = NS(d)
    x = hp(nz(d), 3500) * (np.exp(-np.arange(n) / SR / (d * 0.35)))
    return _fin(x * np.minimum(np.arange(n) / SR / 0.01, 1), vel)


def swell(d=3.0, vel=0.5, lo=300, hi=5000):
    n = NS(d); e = np.sin(np.pi * np.linspace(0, 1, n) ** 1.6) ** 2
    return _fin(bp(nz(d), lo, hi) * e, vel)


def chains(d=0.6, vel=0.6):
    n = NS(d); out = np.zeros(n)
    for _ in range(int(d * 22)):
        t0 = int(R().uniform(0, 0.9) * n)
        f = R().uniform(2600, 5200)
        m = min(NS(0.09), n - t0)
        out[t0:t0 + m] += R().uniform(0.3, 1.0) * partials(f, [1, 1.47, 2.09], [1, 0.6, 0.4], [0.03, 0.025, 0.02], 0.09)[:m]
    return _fin(out, vel)
