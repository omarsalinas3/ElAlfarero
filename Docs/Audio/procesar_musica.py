"""Prepara una canción de Flow Music para el juego: bucle sin corte y ligera en memoria.

Pasos (los mismos que se usaron para menu, nivel1-3 y llorona):
  1. Pasa a 32 kHz (de 48 kHz): la música no necesita más y pesa un tercio menos.
  2. Corta el silencio del final.
  3. Cruce de 3 s del final al inicio con potencia constante: al volver a empezar no se nota el corte.
  4. Normaliza el pico a 0.89 y guarda WAV de 16 bits (va por Git LFS).

Uso (desde la raíz del repo):
    python -I Docs/Audio/procesar_musica.py <entrada.wav> Assets/Audio/Music/<nombre>.wav
Las canciones se bajan completas con https://storage.googleapis.com/producer-app-public/clips/<id>.wav
"""
import sys
from math import gcd

import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly

FRECUENCIA = 32000
CRUCE = 3.0          # segundos
PICO = 0.89


def leer(ruta):
    sr, a = wavfile.read(ruta)
    if a.dtype == np.int16:
        a = a.astype(np.float32) / 32768.0
    elif a.dtype == np.int32:
        a = a.astype(np.float32) / 2147483648.0
    else:
        a = a.astype(np.float32)
    if a.ndim == 1:
        a = a[:, None]
    return sr, a


def main(entrada, salida):
    sr, a = leer(entrada)
    if sr != FRECUENCIA:
        g = gcd(sr, FRECUENCIA)
        a = resample_poly(a, FRECUENCIA // g, sr // g, axis=0).astype(np.float32)

    # Silencio del final: todo lo que quede por debajo de -50 dB
    nivel = np.abs(a).max(axis=1)
    sonando = np.where(nivel > 10 ** (-50 / 20))[0]
    a = a[: sonando[-1] + 1]

    # Cruce final -> inicio: la cola se mezcla sobre el arranque y se quita del final
    n = int(CRUCE * FRECUENCIA)
    t = np.linspace(0.0, np.pi / 2, n)[:, None]
    entra, sale = np.sin(t), np.cos(t)
    cola = a[-n:].copy()
    a = a[:-n].copy()
    a[:n] = a[:n] * entra + cola * sale

    a *= PICO / np.abs(a).max()
    wavfile.write(salida, FRECUENCIA, (a * 32767).astype(np.int16))
    print(f"{salida}: {len(a) / FRECUENCIA:.1f} s, {a.shape[1]} canales, {FRECUENCIA} Hz")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
