# -*- coding: utf-8 -*-
"""Komposisi musik algoritmik (loop mulus) untuk 19 trek Hunter Guild. Jalankan: python3 music_gen.py <outdir> [nama ...]"""
import os, sys, time, random, subprocess, wave
import numpy as np
from synth import *
import inst as I

SCALES = {
    'major': [0, 2, 4, 5, 7, 9, 11], 'dorian': [0, 2, 3, 5, 7, 9, 10], 'minor': [0, 2, 3, 5, 7, 8, 10], 'phrygian': [0, 1, 3, 5, 7, 8, 10],
    'lydian': [0, 2, 4, 6, 7, 9, 11], 'mixo': [0, 2, 4, 5, 7, 9, 10], 'phrygdom': [0, 1, 4, 5, 7, 8, 10], 'harmmin': [0, 2, 3, 5, 7, 8, 11],
    'bone': [0, 1, 3, 6, 7, 8, 10],
}


def deg_note(root, scale, deg):
    sc = SCALES[scale]; o, i = divmod(deg, len(sc))
    return root + sc[i] + 12 * o


RHY = {
    'slow': [[4], [2, 2], [3, 1], [2, 1, 1], [-1, 3]],
    'flow': [[1, 1, 2], [1.5, 0.5, 1, 1], [0.5, 0.5, 1, 2], [2, 1, 1], [1, 0.5, 0.5, 2], [1, 1, 1, 1], [-1, 1, 2]],
    'lively': [[0.5, 0.5, 1, 0.5, 0.5, 1], [1, 0.5, 0.5, 1, 1], [0.5, 0.5, 0.5, 0.5, 1, 1], [1.5, 0.5, 1, 1]],
    'fanfare': [[1.5, 0.5, 1, 1], [0.75, 0.25, 1, 2], [2, 1, 1], [1, 1, 2], [0.5, 0.5, 1, 2]],
    'sparse': [[4], [-2, 2], [-1, 1, -2], [2, -2], [3, -1], [-3, 1]],
}
ARP = {'up': [0, 1, 2, 3], 'updown': [0, 1, 2, 3, 2, 1], 'alberti': [0, 2, 1, 2], 'folk': [0, 2, 1, 2, 0, 2, 1, 2], 'spread': [0, 1, 2, 1],
       'osc': [0, 0, 2, 1], 'down': [3, 2, 1, 0], 'sixt': [0, 1, 2, 1, 0, 1, 2, 3], 'ost': [0, 0, 1, 0, 2, 0, 1, 0], 'tri': [0, 2, 1]}


def on(spec, b):
    if spec is None:
        return True
    if isinstance(spec, tuple):
        return spec[0] <= b < spec[1]
    return any(s[0] <= b < s[1] for s in spec)


def gv(g, b):
    return g(b) if callable(g) else g


class Song:
    def __init__(self, bpm, bars, root, scale, prog, beats=4, seed=1, rt=2.4, damp=4500, wet=1.0, name=''):
        self.bpm = bpm; self.bars = bars; self.root = root; self.scale = scale; self.beats = beats
        self.spb = 60.0 / bpm; self.bar_s = beats * self.spb; self.dur = bars * self.bar_s
        self.prog = [prog[i % len(prog)] for i in range(bars)] if not isinstance(prog, dict) else prog
        self.mix = Mix(self.dur, tail=6.0); self.rnd = random.Random(seed); self.rt = rt; self.damp = damp; self.wet = wet; self.seed = seed
        self.name = name; self._dr = {}
        reseed(seed * 31 + 7)

    def deg(self, b):
        return self.prog[b] if not isinstance(self.prog, dict) else self.prog.get(b, 0)

    def chord(self, b, size=3):
        d = self.deg(b)
        return [deg_note(self.root, self.scale, d + 2 * i) for i in range(size)]

    def T(self, b, beat=0.0):
        return (b * self.beats + beat) * self.spb

    def put(self, x, b, beat, gain=1.0, pan=0.0, rev=0.25, jit=0.0):
        t = self.T(b, beat) + (self.rnd.uniform(-jit, jit) if jit else 0.0)
        self.mix.add(x, max(0.0, t), gain, pan, rev)

    def drum(self, kind, vel):
        key = (kind, round(vel, 1))
        if key not in self._dr:
            f = {'kick': lambda: I.kick(vel), 'snare': lambda: I.snare(vel), 'hat': lambda: I.hat(vel), 'ohat': lambda: I.hat(vel, True),
                 'tom': lambda: I.tom(vel, 100), 'tom2': lambda: I.tom(vel, 140), 'tom3': lambda: I.tom(vel, 80), 'taiko': lambda: I.taiko(vel, self.taiko_f),
                 'fd': lambda: I.framedrum(vel, 150), 'fd2': lambda: I.framedrum(vel, 205), 'fd3': lambda: I.framedrum(vel, 120), 'shaker': lambda: I.shaker(vel),
                 'chains': lambda: I.chains(0.6, vel)}[kind]
            self._dr[key] = f()
        return self._dr[key]

    taiko_f = 62.0

    def render(self):
        return self.mix.render(self.rt, self.damp, self.wet, loop=True, seed=self.seed)


# ------------------------------------------------------------ lapisan
def make_pad(kind, f, d, vel, vowel='a', cut=None):
    if kind == 'strings': return I.strings(f, d, vel, a=1.0, r=1.6, cut=cut or 2200)
    if kind == 'warm': return I.warm(f, d, vel, a=1.4, r=2.2, cut=cut or 1300)
    if kind == 'choir': return I.choir(f, d, vel, vowel=vowel, a=1.1, r=1.6)
    if kind == 'glass': return I.glass(f, d, vel)
    if kind == 'organ': return I.organ(f, d, vel, a=0.4, r=0.8)
    if kind == 'bowed': return I.bowed(f, d, vel, cut=cut or 1500)
    if kind == 'horn': return I.horn(f, d, vel, a=0.35, r=0.8, hi=cut or 2400)
    if kind == 'reed': return I.reed(f, d, vel, a=0.5, r=0.8)
    if kind == 'trem': return I.strings(f, d, vel, a=0.8, r=1.2, cut=cut or 2600, trem=9.0)
    if kind == 'piano': return I.piano(f, d, vel)
    raise ValueError(kind)


def pad(s, kind, oct=0, size=3, vel=0.7, gain=0.4, every=1, bars=None, pan=0.0, rev=0.5, vowel='a', cut=None, spread=0.35, dmul=1.0, notes=None):
    for b in range(0, s.bars, every):
        if not on(bars, b):
            continue
        ch = notes[b] if notes else s.chord(b, size)
        for i, n in enumerate(ch):
            x = make_pad(kind, float(midi(n + oct)), every * s.bar_s * dmul, vel, vowel, cut)
            s.put(x, b, 0, gv(gain, b), pan + spread * (i - (len(ch) - 1) / 2), rev)


def drone(s, note, kind='sub', vel=0.7, gain=0.4, seg=4, bars=None, beat=0.0, rev=0.4, vowel='o', fifth=False):
    for b in range(0, s.bars, seg):
        if not on(bars, b):
            continue
        d = seg * s.bar_s
        notes = [note] + ([note + 7] if fifth else [])
        for n in notes:
            f = float(midi(n))
            x = I.sub(f, d, vel, beat=beat) if kind == 'sub' else (I.choir(f, d, vel, vowel=vowel) if kind == 'choir' else make_pad(kind, f, d, vel))
            s.put(x, b, 0, gv(gain, b), 0.0, rev)


def arp(s, inst, pat='up', rate=0.5, oct=12, size=3, vel=0.6, gain=0.5, bars=None, pan=0.0, rev=0.3, skip=0.0, accent=(1.0, 0.7, 0.85, 0.7), jit=0.006,
        ln=1.6, wide=0.0):
    seq = ARP[pat]
    for b in range(s.bars):
        if not on(bars, b):
            continue
        ch = s.chord(b, size)
        for k in range(int(s.beats / rate + 1e-6)):
            if skip and s.rnd.random() < skip:
                continue
            idx = seq[k % len(seq)]
            n = ch[idx % len(ch)] + 12 * (idx // len(ch)) + oct
            v = vel * accent[k % len(accent)] * s.rnd.uniform(0.9, 1.05)
            x = inst(float(midi(n)), rate * s.spb * ln, v)
            s.put(x, b, k * rate, gv(gain, b), pan + wide * ((k % 4) - 1.5) / 1.5, rev, jit)


def melody(s, inst, rhy='flow', bars=None, lo=7, hi=14, vel=0.8, gain=0.6, pan=0.15, rev=0.35, rest=0.0, seed=1, oct=24, snap=0.7, ln=0.95, seq=(0, 1, 0, 2),
           jit=0.008, kw=None):
    rnd = random.Random(seed * 101 + s.seed)
    memo = {}
    for b in range(s.bars):
        if not on(bars, b):
            continue
        u = b // 2; mid = seq[u % len(seq)]
        if mid not in memo:
            notes = []; deg = (lo + hi) // 2 + rnd.randint(-2, 2)
            for bi in range(2):
                pat = rnd.choice(RHY[rhy]); t = 0.0
                for dur in pat:
                    if dur < 0:
                        t += -dur; continue
                    step = rnd.choice([-2, -1, -1, 0, 1, 1, 2, -3, 3])
                    deg = max(lo, min(hi, deg + step))
                    if rest and rnd.random() < rest and notes:
                        t += dur; continue
                    notes.append((bi * s.beats + t, deg, dur)); t += dur
            memo[mid] = notes
        base = (b % 2) * s.beats
        for (bt, deg, dur) in memo[mid]:
            if bt < base or bt >= base + s.beats:
                continue
            beat = bt - base
            n = deg_note(s.root + oct - 24, s.scale, deg)
            if abs(beat - round(beat)) < 1e-6 and int(round(beat)) % 2 == 0 and rnd.random() < snap:
                pcs = {(m - s.root) % 12 for m in s.chord(b, 3)}
                cand = [m for m in range(n - 3, n + 4) if (m - s.root) % 12 in pcs]
                if cand:
                    n = min(cand, key=lambda m: abs(m - n))
            x = inst(float(midi(n)), dur * s.spb * ln, vel * rnd.uniform(0.9, 1.05), **(kw or {}))
            s.put(x, b, beat, gv(gain, b), pan, rev, jit)


def bass(s, pat='half', oct=-12, kind='cello', vel=0.8, gain=0.45, bars=None, cut=900, rev=0.08):
    def mk(n, d, v):
        f = float(midi(n + oct))
        return I.cello(f, d, v, cut=cut) if kind == 'cello' else I.sub(f, d, v, a=0.02, r=0.15)
    for b in range(s.bars):
        if not on(bars, b):
            continue
        ch = s.chord(b, 3); r_ = ch[0]; f5 = ch[2]; g = gv(gain, b); sp = s.spb
        ev = []
        if pat == 'half': ev = [(0, r_, 2 * sp * 0.95, 1.0), (2, f5 if s.rnd.random() < 0.5 else r_ + 12, 2 * sp * 0.95, 0.8)]
        elif pat == 'whole': ev = [(0, r_, s.bar_s * 0.98, 1.0)]
        elif pat == 'p8': ev = [(k * 0.5, r_ + (12 if k % 4 == 3 else 0), 0.42 * sp, 1.0 if k % 2 == 0 else 0.65) for k in range(int(s.beats * 2))]
        elif pat == 'walk': ev = [(0, r_, sp * 0.9, 1.0), (1, ch[1], sp * 0.9, 0.8), (2, f5, sp * 0.9, 0.9), (3, ch[1], sp * 0.9, 0.8)]
        elif pat == 'gallop': ev = [(0, r_, 0.4 * sp, 1.0), (0.75, r_, 0.2 * sp, 0.7), (1, r_, 0.4 * sp, 0.9), (2, r_, 0.4 * sp, 1.0), (2.75, r_, 0.2 * sp, 0.7), (3, r_, 0.4 * sp, 0.9)]
        elif pat == 'sync': ev = [(0, r_, 1.2 * sp, 1.0), (1.5, r_, 0.8 * sp, 0.8), (2.5, f5, 1.3 * sp, 0.9)]
        for (bt, n, d, v) in ev:
            s.put(mk(n, d, vel * v), b, bt, g, 0.0, rev)


PERC = {
    'handdrum': lambda b: [(0, 'fd3', .8), (1.5, 'fd', .45), (2, 'fd2', .6), (3, 'fd', .5), (3.5, 'fd2', .3)],
    'dumtek': lambda b: [(0, 'fd3', .9), (1.5, 'fd2', .5), (2, 'fd', .6), (2.5, 'fd2', .4), (3, 'fd2', .6)],
    'taiko_slow': lambda b: [(0, 'taiko', .95)] + ([(2.5, 'taiko', .55)] if b % 2 else []),
    'taiko_heavy': lambda b: [(0, 'taiko', 1.0), (2.5, 'taiko', .7), (3.5, 'tom3', .5)],
    'taiko_fast': lambda b: [(k * 0.5, 'taiko', 0.9 if k % 4 == 0 else (0.55 if k % 2 == 0 else 0.35)) for k in range(8)],
    'dirge': lambda b: [(0, 'taiko', .75)] if b % 2 == 0 else [],
    'combat': lambda b: [(0, 'kick', 1.0), (1, 'snare', .85), (1.5, 'kick', .6), (2, 'kick', .9), (3, 'snare', .9)] + [(k * 0.5, 'hat', .4 if k % 2 == 0 else .25) for k in range(8)]
    + ([(3 + k * 0.25, 'tom' if k < 2 else 'tom2', .7) for k in range(4)] if b % 4 == 3 else []),
    'boss': lambda b: [(0, 'taiko', 1.0), (1.5, 'tom', .6), (2, 'taiko', .8), (3, 'tom2', .5), (3.5, 'tom3', .6)] + ([(3 + k * 0.25, 'tom2', .7) for k in range(4)] if b % 4 == 3 else []),
    'tribal': lambda b: [(0, 'tom3', 1.0), (0.75, 'fd3', .5), (1.5, 'tom', .7), (2, 'tom3', .9), (2.5, 'fd', .5), (3, 'tom', .6), (3.5, 'fd2', .5)],
    'stomp': lambda b: [(0, 'kick', 1.0), (1, 'kick', .85), (2, 'kick', 1.0), (3, 'kick', .85), (1.5, 'tom3', .6), (3.5, 'tom3', .7)],
    'gallop': lambda b: [(0, 'kick', 1.0), (0.75, 'tom3', .6), (1, 'kick', .9), (2, 'kick', 1.0), (2.75, 'tom3', .6), (3, 'kick', .9), (3.5, 'taiko', .5)],
    'chain': lambda b: [(0.5, 'chains', .5), (1.5, 'chains', .6), (2.5, 'chains', .5), (3.5, 'chains', .7), (0, 'taiko', .8), (2, 'taiko', .6)] + [(k * 0.5, 'hat', .3) for k in range(8)],
    'shaker': lambda b: [(k * 0.5, 'shaker', .5 if k % 2 == 0 else .3) for k in range(8)],
    'toms_sparse': lambda b: [(0, 'tom3', .8)] if b % 2 == 0 else [(2, 'tom', .5)],
}


def perc(s, name, bars=None, gain=0.6, rev=0.18, pan=0.0, taiko_f=None):
    if taiko_f:
        s.taiko_f = taiko_f
    fn = PERC[name]
    for b in range(s.bars):
        if not on(bars, b):
            continue
        for (bt, kind, v) in fn(b):
            s.put(s.drum(kind, v), b, bt, gv(gain, b), pan, rev, 0.004)


def cymb(s, bars, gain=0.4, lead=2, rev=0.5):
    for b in bars:
        if b < s.bars:
            s.put(I.cymbal(3.2, 0.6), b, 0, gain, 0, rev)
            if lead and b - lead >= 0:
                s.put(I.swell(lead * s.bar_s, 0.45), b - lead, 0, gain * 0.7, 0, rev)


def toll(s, bars, note, gain=0.3, rev=0.7, vel=0.8):
    for b in bars:
        if b < s.bars:
            s.put(I.bell_toll(float(midi(note)), vel), b, 0, gain, 0, rev)


def sparkle(s, bars, prob, lo, hi, gain=0.25, inst=None, rev=0.7, oct=48):
    inst = inst or I.celesta
    for b in range(s.bars):
        if not on(bars, b):
            continue
        for k in range(int(s.beats)):
            if s.rnd.random() < prob:
                deg = s.rnd.randint(lo, hi)
                s.put(inst(float(midi(deg_note(s.root + oct - 48, s.scale, deg))), 0.3, s.rnd.uniform(0.5, 0.9)), b, k + s.rnd.choice([0, 0.5]), gain, s.rnd.uniform(-0.6, 0.6), rev)


def bamboo_rand(s, bars, prob, gain=0.3):
    for b in range(s.bars):
        if not on(bars, b):
            continue
        for k in range(int(s.beats * 2)):
            if s.rnd.random() < prob:
                s.put(I.bamboo(s.rnd.uniform(0.5, 0.9)), b, k * 0.5, gain, s.rnd.uniform(-0.7, 0.7), 0.4)


def ramp(b0, b1, g0, g1):
    return lambda b: g0 + (g1 - g0) * min(1.0, max(0.0, (b - b0) / float(b1 - b0)))


def updown(up0, up1, dn0, dn1, g):
    return lambda b: g * min(1.0, max(0.0, (b - up0) / float(max(1, up1 - up0)))) * (1.0 - min(1.0, max(0.0, (b - dn0) / float(max(1, dn1 - dn0)))))


# ------------------------------------------------------------ 19 trek
def t_town():
    s = Song(96, 32, 50, 'major', [0, 4, 5, 3, 0, 3, 4, 0] * 3 + [1, 1, 4, 0, 0, 3, 4, 0], seed=11, rt=1.7, wet=0.8, name='town')
    pad(s, 'warm', 0, 3, 0.6, 0.22, every=2, rev=0.5)
    bass(s, 'half', -12, 'cello', 0.8, 0.42)
    arp(s, I.lute, 'folk', 0.5, 12, 3, 0.62, 0.5, bars=[(0, 32)], pan=-0.25, rev=0.25, skip=0.04)
    melody(s, I.flute, 'flow', [(4, 16), (20, 32)], 6, 13, 0.85, 0.7, pan=0.2, rev=0.45, rest=0.08, seed=3, oct=36)
    arp(s, I.pizz, 'tri', 1.0, 12, 3, 0.6, 0.34, bars=[(8, 32)], pan=0.3, rev=0.2, accent=(1.0, 0.6, 0.8))
    perc(s, 'handdrum', [(8, 32)], 0.42)
    perc(s, 'shaker', [(16, 32)], 0.2)
    return s


def t_town_night():
    s = Song(64, 24, 53, 'major', [0, 5, 3, 4] * 6, seed=12, rt=3.0, wet=1.0, name='town_night')
    pad(s, 'warm', 0, 3, 0.6, 0.3, every=2, rev=0.6, cut=1100)
    arp(s, I.piano, 'spread', 1.0, 0, 3, 0.55, 0.5, bars=[(0, 24)], pan=-0.1, rev=0.5, ln=1.0)
    melody(s, I.celesta, 'sparse', [(4, 24)], 8, 15, 0.7, 0.55, pan=0.25, rev=0.7, rest=0.3, seed=5, oct=48, snap=0.9)
    bass(s, 'whole', -12, 'cello', 0.7, 0.3, bars=[(4, 24)], cut=700)
    return s


def t_guild():
    s = Song(80, 24, 55, 'dorian', [0, 3, 0, 6] * 6, seed=13, rt=1.4, wet=0.7, name='guild')
    pad(s, 'reed', 0, 3, 0.45, 0.2, every=1, rev=0.35)
    bass(s, 'whole', -12, 'cello', 0.8, 0.4, cut=800)
    arp(s, I.lute, 'folk', 0.5, 0, 3, 0.62, 0.55, bars=[(0, 24)], pan=-0.2, rev=0.25, skip=0.06)
    melody(s, I.lute, 'flow', [(8, 24)], 7, 13, 0.8, 0.55, pan=0.2, rev=0.3, rest=0.15, seed=7, oct=24)
    melody(s, I.flute, 'sparse', [(16, 24)], 9, 15, 0.7, 0.4, pan=0.3, rev=0.4, rest=0.2, seed=8, oct=36)
    return s


def t_eldrath():
    s = Song(70, 28, 52, 'minor', [0, 5, 3, 6] * 7, seed=14, rt=3.4, wet=1.1, name='eldrath')
    pad(s, 'strings', 0, 3, 0.7, 0.38, every=2, rev=0.6, cut=2000)
    melody(s, I.horn, 'slow', [(8, 28)], 4, 10, 0.8, 0.5, pan=0.0, rev=0.5, rest=0.1, seed=9, oct=12, snap=0.9)
    arp(s, I.harp, 'up', 1.0, 24, 3, 0.55, 0.36, bars=[(4, 28)], pan=0.2, rev=0.6, skip=0.2, ln=2.5)
    perc(s, 'dirge', [(8, 28)], 0.5, taiko_f=55)
    toll(s, [0, 8, 16, 24], 40, 0.3)
    sparkle(s, [(8, 28)], 0.12, 7, 16, 0.2, rev=0.8, oct=72)
    return s


def t_forest():
    s = Song(80, 24, 57, 'lydian', [0, 1, 4, 0, 0, 1, 3, 0, 4, 1, 0, 0] * 2, seed=15, rt=2.4, wet=0.9, name='forest')
    pad(s, 'strings', 0, 3, 0.55, 0.27, every=2, rev=0.6, cut=1800)
    melody(s, I.flute, 'flow', [(2, 11), (14, 22)], 6, 13, 0.85, 0.65, pan=0.15, rev=0.5, rest=0.12, seed=15, oct=36)
    arp(s, I.harp, 'updown', 0.5, 12, 3, 0.6, 0.42, bars=[(0, 11), (14, 24)], pan=-0.2, rev=0.45, skip=0.12, ln=2.0)
    bass(s, 'whole', -12, 'cello', 0.6, 0.25, bars=[(4, 11), (14, 24)], cut=700)
    return s


def t_desert():
    s = Song(90, 32, 50, 'phrygdom', [0, 0, 1, 0, 0, 0, 3, 0] * 4, seed=16, rt=1.8, wet=0.8, name='desert')
    drone(s, 38, 'strings', 0.6, 0.3, seg=2, fifth=True)
    arp(s, I.oud, 'ost', 0.5, 12, 3, 0.7, 0.5, bars=[(0, 32)], pan=-0.2, rev=0.3, skip=0.08)
    melody(s, I.flute, 'flow', [(8, 32)], 6, 14, 0.85, 0.55, pan=0.25, rev=0.5, rest=0.12, seed=16, oct=36, snap=0.5)
    perc(s, 'dumtek', [(4, 32)], 0.5)
    perc(s, 'shaker', [(12, 32)], 0.2)
    return s


def t_swamp():
    s = Song(60, 24, 49, 'phrygian', [0, 0, 1, 0] * 6, seed=17, rt=3.0, wet=1.0, name='swamp')
    pad(s, 'warm', 0, 3, 0.55, 0.32, every=2, rev=0.6, cut=700)
    bass(s, 'sync', -12, 'cello', 0.8, 0.4, bars=[(0, 24)], cut=500)
    bamboo_rand(s, [(2, 24)], 0.12, 0.32)
    for b in (3, 9, 15, 20):
        n = s.chord(b)[0] + 24
        s.put(I.gliss(float(midi(n)), float(midi(n + (3 if b % 2 else -4))), 3 * s.bar_s * 0.8, 0.6, a=0.8, r=1.2), b, 0, 0.26, 0.2, 0.6)
    pad(s, 'choir', 0, 2, 0.5, 0.2, every=2, bars=[(8, 24)], vowel='u', rev=0.7)
    return s


def t_tower():
    s = Song(80, 32, 47, 'minor', [0, 6, 5, 6] * 8, seed=18, rt=3.2, wet=1.1, name='tower')
    g = updown(0, 20, 24, 32, 1.0)
    pad(s, 'choir', 12, 3, 0.6, lambda b: 0.38 * g(b) + 0.02, every=2, vowel='o', rev=0.7)
    pad(s, 'trem', 12, 3, 0.6, lambda b: 0.3 * g(b), every=2, rev=0.5, cut=2400)
    drone(s, 35, 'sub', 0.6, lambda b: 0.25 + 0.1 * g(b), seg=4)
    perc(s, 'dirge', [(4, 28)], lambda b: 0.5 * g(b), taiko_f=55)
    sparkle(s, [(4, 30)], 0.12, 7, 16, 0.22, I.chime, oct=72)
    return s


def t_volcano():
    s = Song(100, 32, 40, 'phrygian', [0, 0, 1, 0, 0, 5, 1, 0] * 4, seed=19, rt=2.2, wet=0.9, name='volcano')
    drone(s, 28, 'sub', 0.7, 0.4, seg=4)
    pad(s, 'warm', 12, 3, 0.6, 0.28, every=2, rev=0.5, cut=1100)
    pad(s, 'horn', 12, 2, 0.7, 0.3, every=2, bars=[(4, 32)], rev=0.5, cut=1800, notes={b: [s.chord(b)[0], s.chord(b)[2]] for b in range(32)})
    perc(s, 'taiko_heavy', [(0, 32)], 0.75, taiko_f=60)
    cymb(s, [8, 16, 24], 0.3)
    return s


def t_bone():
    s = Song(50, 16, 42, 'bone', [0, 1, 0, 4], seed=20, rt=4.2, wet=1.3, name='bone')
    for i, n in enumerate([66, 67, 66, 70]):
        s.put(I.glass(float(midi(n)), 4 * s.bar_s * 0.8, 0.7, a=1.8, r=3.0), 4 * i, 0, 0.5, 0.0, 0.8)
    for b, n in [(2, 78), (6, 79), (10, 78), (14, 82)]:
        s.put(I.bowed(float(midi(n)), 5.0, 0.6, a=2.0, r=2.5, cut=1800), b, 0, 0.28, s.rnd.uniform(-0.5, 0.5), 0.8)
    drone(s, 30, 'sub', 0.5, 0.22, seg=8)
    toll(s, [0, 8], 42, 0.25)
    return s


def t_demon():
    s = Song(45, 16, 36, 'harmmin', [0, 0, 1, 0], seed=21, rt=3.8, wet=1.1, name='demon')
    drone(s, 24, 'sub', 0.7, 0.4, seg=4, beat=1.3)
    pad(s, 'choir', 12, 2, 0.6, 0.38, every=4, vowel='o', rev=0.7, notes={b: [36 + 0, 36 + 7, 36 + 13] for b in range(16)})
    pad(s, 'bowed', 24, 1, 0.5, 0.18, every=4, bars=[(8, 16)], rev=0.8, cut=1200, notes={b: [42] for b in range(16)})
    perc(s, 'dirge', [(0, 16)], 0.55, taiko_f=45)
    return s


def t_combat():
    s = Song(136, 40, 50, 'minor', [0, 0, 5, 6, 0, 0, 3, 4] * 5, seed=22, rt=1.3, wet=0.6, name='combat')
    perc(s, 'combat', [(0, 40)], 0.62)
    bass(s, 'p8', -12, 'cello', 0.8, 0.5, cut=800)
    arp(s, lambda f, d, v: I.strings(f, 0.14, v, a=0.01, r=0.1, cut=2600, voices=2), 'ost', 0.5, 12, 3, 0.65, 0.4, bars=[(4, 40)], pan=0.2, rev=0.2)
    arp(s, lambda f, d, v: I.horn(f, 0.45, v, a=0.03, r=0.15), 'osc', 4.0, 0, 3, 0.7, 0.4, bars=[(8, 40)], rev=0.3)
    melody(s, I.horn, 'fanfare', [(16, 32)], 7, 13, 0.8, 0.45, pan=0.0, rev=0.3, seed=22, oct=24, snap=0.9)
    cymb(s, [16, 32], 0.3, lead=1)
    return s


def t_boss():
    s = Song(120, 36, 49, 'minor', [0, 5, 2, 6, 0, 5, 3, 4] * 5, seed=23, rt=2.0, wet=0.8, name='boss')
    pad(s, 'choir', 0, 3, 0.7, 0.38, every=2, vowel='a', rev=0.6)
    pad(s, 'choir', -12, 2, 0.7, 0.28, every=2, vowel='o', rev=0.6, bars=[(8, 36)])
    arp(s, lambda f, d, v: I.strings(f, 0.16, v, a=0.01, r=0.1, cut=2400, voices=2), 'ost', 0.5, 12, 3, 0.65, 0.4, bars=[(4, 36)], pan=0.2, rev=0.25)
    perc(s, 'boss', [(0, 36)], 0.75, taiko_f=58)
    bass(s, 'p8', -12, 'cello', 0.8, 0.45, bars=[(8, 36)], cut=700)
    melody(s, I.horn, 'slow', [(8, 36)], 5, 11, 0.8, 0.5, pan=0.0, rev=0.4, seed=23, oct=12, snap=0.9)
    cymb(s, [8, 16, 24, 32], 0.35)
    return s


def t_boss_ogre_chief():
    s = Song(90, 32, 45, 'phrygian', [0, 0, 1, 0] * 8, seed=24, rt=1.8, wet=0.7, name='boss_ogre_chief')
    perc(s, 'tribal', [(0, 32)], 0.85)
    perc(s, 'taiko_slow', [(8, 32)], 0.7, taiko_f=52)
    drone(s, 33, 'sub', 0.7, 0.4, seg=4)
    arp(s, lambda f, d, v: I.horn(f, 0.4, v, a=0.04, r=0.2, hi=1800), 'osc', 1.5, 0, 3, 0.8, 0.5, bars=[(4, 32)], rev=0.3, accent=(1.0, 0.8, 0.9, 0.8))
    pad(s, 'warm', 0, 3, 0.6, 0.22, every=2, rev=0.5, cut=800, bars=[(8, 32)])
    return s


def t_boss_behemoth():
    s = Song(110, 32, 41, 'minor', [0, 0, 6, 5] * 8, seed=25, rt=1.9, wet=0.7, name='boss_behemoth')
    perc(s, 'gallop', [(0, 32)], 0.85, taiko_f=50)
    bass(s, 'gallop', -12, 'sub', 0.9, 0.55)
    pad(s, 'horn', 0, 3, 0.7, 0.35, every=1, bars=[(4, 32)], rev=0.4, cut=2200)
    pad(s, 'warm', 0, 3, 0.5, 0.2, every=2, rev=0.5, cut=900)
    cymb(s, [8, 16, 24], 0.3)
    return s


def t_boss_cerberus():
    s = Song(130, 36, 44, 'phrygdom', [0, 0, 1, 0] * 9, seed=26, rt=1.7, wet=0.7, name='boss_cerberus')
    arp(s, lambda f, d, v: I.cello(f, 0.22, v, a=0.02, r=0.1, cut=900), 'ost', 0.5, -12, 3, 0.8, 0.5, bars=[(0, 36)], rev=0.2)
    arp(s, lambda f, d, v: I.strings(f, 0.14, v, a=0.01, r=0.08, cut=2600, voices=2), 'sixt', 0.25, 12, 3, 0.6, 0.3, bars=[(8, 36)], pan=0.3, rev=0.25)
    arp(s, I.pizz, 'tri', 0.75, 24, 3, 0.7, 0.35, bars=[(16, 36)], pan=-0.3, rev=0.3)
    perc(s, 'chain', [(0, 36)], 0.7, taiko_f=52)
    pad(s, 'choir', 0, 3, 0.6, 0.3, every=2, vowel='o', bars=[(16, 36)], rev=0.7)
    cymb(s, [8, 16, 24, 32], 0.3, lead=1)
    return s


def t_boss_ignarok():
    s = Song(125, 32, 52, 'phrygdom', [0, 0, 1, 0, 0, 3, 1, 0] * 4, seed=27, rt=2.0, wet=0.8, name='boss_ignarok')
    perc(s, 'taiko_fast', [(0, 32)], 0.7, taiko_f=60)
    melody(s, I.horn, 'fanfare', [(8, 32)], 5, 12, 0.85, 0.5, pan=0.0, rev=0.4, seed=27, oct=24, snap=0.9)
    pad(s, 'choir', 12, 3, 0.7, 0.35, every=2, vowel='a', rev=0.7)
    arp(s, lambda f, d, v: I.strings(f, 0.14, v, a=0.01, r=0.08, cut=2800, voices=2), 'sixt', 0.25, 24, 3, 0.55, 0.28, bars=[(16, 32)], pan=0.25, rev=0.3)
    bass(s, 'p8', -12, 'sub', 0.9, 0.45, bars=[(4, 32)])
    cymb(s, [8, 16, 24], 0.35, lead=1)
    return s


def t_boss_bone_king():
    s = Song(70, 24, 50, 'minor', [0, 5, 3, 4] * 6, seed=28, rt=3.2, wet=1.1, name='boss_bone_king')
    arp(s, I.piano, 'spread', 2.0, -12, 3, 0.7, 0.55, bars=[(0, 24)], rev=0.5, ln=1.0)
    melody(s, I.piano, 'sparse', [(4, 24)], 6, 13, 0.75, 0.5, pan=0.15, rev=0.5, rest=0.2, seed=28, oct=36, snap=0.9)
    pad(s, 'choir', 0, 3, 0.6, 0.36, every=2, vowel='o', rev=0.7)
    pad(s, 'strings', 12, 3, 0.5, 0.22, every=2, rev=0.6, cut=1800, bars=[(8, 24)])
    toll(s, [0, 4, 8, 12, 16, 20], 38, 0.28)
    perc(s, 'dirge', [(4, 24)], 0.4, taiko_f=50)
    return s


def t_boss_demon_lord():
    s = Song(140, 40, 48, 'harmmin', [0, 0, 5, 4, 0, 0, 3, 4] * 5, seed=29, rt=2.2, wet=0.8, name='boss_demon_lord')
    ph2 = lambda g: (lambda b: g if 20 <= b < 36 else 0.0)
    pad(s, 'choir', 0, 3, 0.7, 0.4, every=2, vowel='o', rev=0.7)
    pad(s, 'organ', 0, 3, 0.55, 0.26, every=2, rev=0.5)
    bass(s, 'p8', -12, 'sub', 0.9, 0.5)
    perc(s, 'taiko_slow', [(0, 20), (36, 40)], 0.6, taiko_f=52)
    perc(s, 'boss', [(20, 36)], 0.8, taiko_f=55)
    perc(s, 'combat', [(28, 36)], 0.4)
    arp(s, lambda f, d, v: I.strings(f, 0.14, v, a=0.01, r=0.08, cut=2600, voices=2), 'sixt', 0.25, 24, 3, 0.55, ph2(0.3), pan=0.25, rev=0.3)
    arp(s, lambda f, d, v: I.horn(f, 0.5, v, a=0.03, r=0.2), 'osc', 4.0, 0, 3, 0.8, ph2(0.42), rev=0.3)
    melody(s, I.organ, 'fanfare', [(24, 36)], 6, 13, 0.8, 0.35, pan=0.0, rev=0.4, seed=29, oct=24, snap=0.9)
    cymb(s, [20, 28], 0.35, lead=1)
    return s


TRACKS = {f.__name__[2:]: f for f in [t_town, t_town_night, t_guild, t_eldrath, t_forest, t_desert, t_swamp, t_tower, t_volcano, t_bone, t_demon, t_combat, t_boss,
                                      t_boss_ogre_chief, t_boss_behemoth, t_boss_cerberus, t_boss_ignarok, t_boss_bone_king, t_boss_demon_lord]}

RMS = {'town': -17, 'town_night': -19, 'guild': -17, 'eldrath': -17, 'forest': -17, 'desert': -17, 'swamp': -19, 'tower': -18, 'volcano': -16, 'bone': -21,
       'demon': -19, 'combat': -15, 'boss': -15, 'boss_ogre_chief': -15, 'boss_behemoth': -15, 'boss_cerberus': -15, 'boss_ignarok': -15, 'boss_bone_king': -17,
       'boss_demon_lord': -15}


def write_wav_st(path, x):
    pcm = (np.clip(x, -1, 1) * 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())


def render_track(name, outdir, q=4):
    t0 = time.time()
    s = TRACKS[name]()
    x = master(s.render(), RMS.get(name, -19), loop=True)
    os.makedirs(outdir, exist_ok=True)
    wav = os.path.join(outdir, 'music_' + name + '.wav'); ogg = os.path.join(outdir, 'music_' + name + '.ogg')
    write_wav_st(wav, x)
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', wav, '-c:a', 'libvorbis', '-q:a', str(q), ogg], check=True)
    os.remove(wav)
    seam = float(np.max(np.abs(x[0] - x[-1])))
    return dict(dur=len(x) / SR, peak=float(np.max(np.abs(x))), rms=float(20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-9)), seam=seam,
                kb=os.path.getsize(ogg) // 1024, sec=time.time() - t0)


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else 'out_music'
    names = sys.argv[2:] or list(TRACKS)
    for n in names:
        r = render_track(n, out)
        print(f"{n:18s} {r['dur']:5.1f}s peak={r['peak']:.2f} rms={r['rms']:6.1f}dB seam={r['seam']:.3f} {r['kb']}KB ({r['sec']:.0f}s)", flush=True)
