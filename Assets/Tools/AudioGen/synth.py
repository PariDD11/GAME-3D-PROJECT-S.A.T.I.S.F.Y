# -*- coding: utf-8 -*-
"""Mesin sintesis kecil (numpy/scipy) untuk SFX dan musik Hunter Guild. Semua suara dibuat dari nol (tanpa sampel pihak lain)."""
import numpy as np
from scipy import signal

SR = 44100
_rng = np.random.default_rng(20261105)


def R():
    return _rng


def reseed(s):
    global _rng
    _rng = np.random.default_rng(s)


def NS(d):
    return int(d * SR + 1e-6)


def nz(d):
    return _rng.standard_normal(NS(d))


def tt(d):
    return np.arange(NS(d)) / SR


# ---------------------------------------------------------------- filter
def lp(x, f, o=2):
    return signal.sosfilt(signal.butter(o, min(f, SR * 0.45), 'low', fs=SR, output='sos'), x, axis=0)


def hp(x, f, o=2):
    return signal.sosfilt(signal.butter(o, max(10.0, min(f, SR * 0.45)), 'high', fs=SR, output='sos'), x, axis=0)


def bp(x, f1, f2, o=2):
    f1 = max(20.0, f1); f2 = min(f2, SR * 0.45)
    return signal.sosfilt(signal.butter(o, [f1, f2], 'band', fs=SR, output='sos'), x, axis=0)


def norm(x, peak=0.8):
    m = float(np.max(np.abs(x))) + 1e-9
    return x * (peak / m)


# ---------------------------------------------------------------- envelope
def env_exp(n, tau, a=0.002):
    t = np.arange(n) / SR
    return np.minimum(t / max(a, 1e-4), 1.0) * np.exp(-t / tau)


def env_sus(d, a, r):
    """Panjang d detik (tahan) + r detik pelepasan; naik/turun berbentuk kosinus."""
    n = int(round((d + r) * SR)); t = np.arange(n) / SR
    up = 0.5 - 0.5 * np.cos(np.pi * np.clip(t / max(a, 1e-4), 0, 1))
    dn = 0.5 + 0.5 * np.cos(np.pi * np.clip((t - d) / max(r, 1e-4), 0, 1))
    return up * dn


def fit(e, n):
    if len(e) >= n:
        return e[:n]
    return np.concatenate([e, np.zeros(n - len(e))])


def env_bell(n, peak=0.5):
    """Bukit halus (untuk whoosh); puncak di posisi 'peak' (0..1)."""
    x = np.linspace(0, 1, n)
    xx = x ** (np.log(0.5) / np.log(max(0.05, min(0.95, peak))))
    return np.sin(np.pi * xx) ** 2


def midi(n):
    return 440.0 * 2 ** ((np.asarray(n, dtype=float) - 69) / 12.0)


# ---------------------------------------------------------------- generator dasar
def sweep(f0, f1, d, curve='exp'):
    n = NS(d); t = np.arange(n) / SR
    f = f0 * (f1 / f0) ** (t / d) if curve == 'exp' else f0 + (f1 - f0) * t / d
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def sweep_noise(f0, f1, d, q=0.6, bands=7, shape=None, seed=None):
    """Derau yang pusat frekuensinya menyapu f0->f1 (whoosh)."""
    n = NS(d)
    x = nz(d)
    cs = f0 * (f1 / f0) ** (np.arange(bands) / max(1, bands - 1))
    out = np.zeros(n); t = np.linspace(0, 1, n)
    for i, c in enumerate(cs):
        tc = i / max(1, bands - 1)
        w = np.exp(-0.5 * ((t - tc) / (0.5 / bands + 0.06)) ** 2)
        out += w * bp(x, c * (1 - q * 0.5), min(c * (1 + q), SR * 0.45))
    if shape is not None:
        out *= fit(shape, n)
    return out


def addv(f, n, amp_fn, H=24, vib_rate=5.3, vib_depth=0.0, vib_delay=0.3, detune=0.0):
    t = np.arange(n) / SR
    fd = 2 ** (detune / 1200.0)
    if vib_depth:
        vd = vib_depth * np.clip((t - vib_delay) / 0.6, 0, 1)
        ph = 2 * np.pi * np.cumsum(f * fd * (1 + vd * np.sin(2 * np.pi * vib_rate * t))) / SR
    else:
        ph = 2 * np.pi * f * fd * t
    out = np.zeros(n)
    phs = _rng.uniform(0, 2 * np.pi, H + 1)
    for k in range(1, H + 1):
        fk = k * f * fd
        if fk > SR * 0.45:
            break
        a = amp_fn(k, fk, t)
        out += a * np.sin(k * ph + phs[k])
    return out


def partials(f, ratios, amps, taus, d, a=0.002):
    n = NS(d); t = np.arange(n) / SR
    out = np.zeros(n)
    for r, am, tau in zip(ratios, amps, taus):
        fk = f * r
        if fk > SR * 0.45:
            continue
        out += am * np.exp(-t / tau) * np.sin(2 * np.pi * fk * t + _rng.uniform(0, 6.28))
    out = out * np.minimum(t / max(a, 1e-4), 1.0)
    k = min(NS(0.06), n // 4)
    if k > 2:
        out[-k:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, k))
    return out


def formant_gain(fk, forms):
    g = 0.03
    for (F, B, G) in forms:
        g = g + G * np.exp(-0.5 * ((fk - F) / B) ** 2)
    return g


VOWELS = {
    'a': [(800, 110, 1.0), (1150, 130, 0.7), (2900, 200, 0.25), (3500, 250, 0.12)],
    'o': [(450, 80, 1.0), (800, 100, 0.6), (2830, 200, 0.18), (3500, 250, 0.1)],
    'u': [(325, 70, 1.0), (700, 90, 0.5), (2530, 200, 0.12)],
    'e': [(400, 80, 1.0), (1700, 150, 0.6), (2600, 200, 0.2)],
    'uh': [(600, 100, 1.0), (1000, 120, 0.8), (2500, 200, 0.2)],
    'ah': [(730, 100, 1.0), (1090, 120, 0.8), (2440, 200, 0.3)],
}


def vocal(f0, d, vowel='a', vib=0.0, rough=0.0, breath=0.02, f1=None, r=0.05, a=0.03, glide_curve=None):
    """Suara bernada (formant additive). f0 bisa berubah f0->f1 selama d."""
    n = NS(d); t = np.arange(n) / SR
    forms = VOWELS[vowel] if isinstance(vowel, str) else vowel
    fm = np.full(n, f0, dtype=float) if f1 is None else f0 * (f1 / f0) ** (t / d)
    if vib:
        fm = fm * (1 + vib * np.sin(2 * np.pi * 5.5 * t))
    if rough:
        fm = fm * (1 + rough * np.sin(2 * np.pi * 28 * t) + 0.5 * rough * _rng.standard_normal(n) * 0.2)
    ph = 2 * np.pi * np.cumsum(fm) / SR
    out = np.zeros(n)
    H = 40
    for k in range(1, H + 1):
        fk = k * fm
        g = (1.0 / k ** 0.9) * formant_gain(fk, forms)
        g = np.where(fk < SR * 0.45, g, 0.0)
        out += g * np.sin(k * ph + _rng.uniform(0, 6.28))
    if breath:
        out += breath * np.max(np.abs(out)) * bp(nz(n / SR), 500, 3500)
    return out * fit(env_sus(max(d - r, 0.01), a, r), n)


# ---------------------------------------------------------------- reverb
_irc = {}


def ir(rt60, damp=5000.0, seed=1):
    key = (round(rt60, 2), round(damp), seed)
    if key in _irc:
        return _irc[key]
    n = int(rt60 * 1.3 * SR); t = np.arange(n) / SR
    chans = []
    for ch in (0, 1):
        r = np.random.default_rng(seed * 17 + ch)
        x = r.standard_normal(n)
        lo = lp(x, 1000, 1)
        hi = lp(hp(x, 1000, 1), damp, 1)
        y = lo * np.exp(-6.91 * t / rt60) + hi * np.exp(-6.91 * t / (rt60 * 0.55))
        y *= 1 - np.exp(-t / 0.015)
        chans.append(y / np.sqrt(np.sum(y ** 2)))
    out = np.stack(chans, 1)
    _irc[key] = out
    return out


def reverb_mono(x, rt60=1.6, damp=5000.0, seed=1):
    h = ir(rt60, damp, seed)
    return np.stack([signal.fftconvolve(x, h[:, 0]), signal.fftconvolve(x, h[:, 1])], 1)


def softclip(x, drive=1.0):
    return np.tanh(x * drive) / drive


# ---------------------------------------------------------------- mixer
class Mix:
    def __init__(self, dur, tail=5.0):
        self.dur = dur; self.tail = tail
        n = int((dur + tail) * SR)
        self.dry = np.zeros((n, 2)); self.send = np.zeros(n)

    def add(self, x, t0, gain=1.0, pan=0.0, rev=0.25):
        i = int(round(t0 * SR))
        if i < 0:
            x = x[-i:]; i = 0
        m = min(len(x), len(self.dry) - i)
        if m <= 0:
            return
        th = (np.clip(pan, -1, 1) + 1) * np.pi / 4
        seg = x[:m] * gain
        self.dry[i:i + m, 0] += seg * np.cos(th)
        self.dry[i:i + m, 1] += seg * np.sin(th)
        if rev:
            self.send[i:i + m] += seg * rev

    def render(self, rt60=2.4, damp=4500.0, wet=1.0, loop=True, seed=1):
        n = len(self.dry)
        w = reverb_mono(self.send, rt60, damp, seed)[:n] if wet > 0 else 0
        mix = self.dry + wet * (w if wet > 0 else 0)
        nl = int(self.dur * SR)
        if loop:
            out = mix[:nl].copy()
            tail = mix[nl:]
            m = min(len(tail), nl)
            out[:m] += tail[:m]
        else:
            out = mix
        return out


def master(x, rms_db=-19.0, peak=0.89, drive=1.15, loop=False):
    x = x - np.mean(x, axis=0)
    sos = signal.butter(2, 28, 'high', fs=SR, output='sos')
    if loop:
        # penyaring causal yang dimulai dari nol membuat awal dan akhir loop tidak nyambung (bunyi "tik" tiap putaran):
        # jalankan di atas 3 salinan berurutan lalu ambil yang tengah, jadi keadaan penyaring sudah "hangat" dan loop periodik
        n = len(x)
        x = signal.sosfilt(sos, np.concatenate([x, x, x]), axis=0)[n:2 * n]
    else:
        x = signal.sosfilt(sos, x, axis=0)
    rms = np.sqrt(np.mean(x ** 2)) + 1e-9
    g = 10 ** (rms_db / 20) / rms
    y = softclip(x * g, drive)
    pk = np.max(np.abs(y))
    if pk > peak:
        y *= peak / pk
    return y
