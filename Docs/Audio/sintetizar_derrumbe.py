"""Sintetiza los sonidos del derrumbe de mina (#88), como el resto de los efectos del juego (sin muestras externas).

    derrumbe.wav        retumbo grave de ~2.2 s con crujidos de roca (aviso de cada tanda)
    roca_impacto_1/2    golpe seco de roca contra el suelo con grava

Uso (desde la raíz del repo):  python -I Docs/Audio/sintetizar_derrumbe.py
"""
import os
import wave
import numpy as np

SR = 44100
DESTINO = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Audio", "SFX")


def guardar(nombre, x):
    x = x / max(1e-6, np.max(np.abs(x))) * 0.9
    with wave.open(os.path.join(DESTINO, nombre + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
    print(nombre)


def pasa_bajos(x, corte):
    # Filtro de un polo: suficiente para oscurecer ruido
    a = np.exp(-2 * np.pi * corte / SR)
    y = np.zeros_like(x)
    for i in range(1, len(x)):
        y[i] = (1 - a) * x[i] + a * y[i - 1]
    return y


def derrumbe(rng):
    n = int(SR * 2.2)
    t = np.arange(n) / SR
    ruido = pasa_bajos(rng.standard_normal(n), 90)
    retumbo = ruido * (0.6 + 0.4 * np.sin(2 * np.pi * 3.1 * t)) + 0.5 * np.sin(2 * np.pi * 38 * t) * pasa_bajos(rng.standard_normal(n), 6)
    envolvente = np.minimum(1, t / 0.35) * np.minimum(1, (2.2 - t) / 0.8)
    x = retumbo * envolvente
    for _ in range(14):                                    # Crujidos de roca sueltos
        i = rng.integers(int(0.2 * SR), n - 4000)
        m = 2500
        x[i:i + m] += pasa_bajos(rng.standard_normal(m), 1400) * np.exp(-np.arange(m) / 500) * rng.uniform(0.6, 1.4)
    return x


def impacto(rng):
    n = int(SR * 0.6)
    t = np.arange(n) / SR
    golpe = np.sin(2 * np.pi * 70 * t * np.exp(-t * 4)) * np.exp(-t * 14)
    grava = pasa_bajos(rng.standard_normal(n), 2200) * np.exp(-t * 9) * 0.7
    return golpe + grava


if __name__ == "__main__":
    rng = np.random.default_rng(88)
    guardar("derrumbe", derrumbe(rng))
    guardar("roca_impacto_1", impacto(rng))
    guardar("roca_impacto_2", impacto(rng))
