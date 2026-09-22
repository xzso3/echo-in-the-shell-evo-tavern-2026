"""Regenerate NativeDemo's original, procedural placeholder audio (Python 3 stdlib)."""

from math import cos, exp, pi, sin
from pathlib import Path
from random import Random
from struct import pack
import wave


RATE = 22050
DEST = Path(__file__).resolve().parent


def write(name, samples, peak=0.78):
    # End on a zero sample so short effects cannot click when AudioSource stops.
    fade = min(round(RATE * 0.01), len(samples))
    for offset in range(1, fade + 1):
        samples[-offset] *= (offset - 1) / max(1, fade - 1)
    largest = max(abs(sample) for sample in samples)
    scale = peak / largest if largest else 0
    with wave.open(str(DEST / name), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(b"".join(pack("<h", round(max(-1, min(1, sample * scale)) * 32767)) for sample in samples))


def bgm():
    # Four 4-second bars. Envelopes end at zero, including the final loop seam.
    chords = ((220.00, 261.63, 329.63), (174.61, 220.00, 261.63),
              (196.00, 261.63, 329.63), (196.00, 246.94, 293.66))
    bass = (110.00, 87.31, 98.00, 98.00)
    rng = Random(230923)
    samples = []
    duration = 16.0
    for index in range(round(duration * RATE)):
        t = index / RATE
        bar = min(int(t / 4), 3)
        bar_time = t - 4 * bar
        beat_time = t % 0.5
        eighth_time = t % 0.25
        note_index = int(bar_time / 0.25) % 8
        note = chords[bar][(0, 1, 2, 1, 0, 1, 2, 1)[note_index]] * 2

        pad_envelope = sin(pi * bar_time / 4) ** 2
        pad = sum(sin(2 * pi * frequency * t) + 0.18 * sin(4 * pi * frequency * t)
                  for frequency in chords[bar]) * 0.048 * pad_envelope
        pluck_envelope = (1 - exp(-eighth_time * 120)) * exp(-eighth_time * 16)
        pluck = (sin(2 * pi * note * t) + 0.2 * sin(4 * pi * note * t)) * 0.105 * pluck_envelope
        bass_envelope = (1 - exp(-beat_time * 100)) * exp(-beat_time * 5)
        low = sin(2 * pi * bass[bar] * t) * 0.10 * bass_envelope
        hat = rng.uniform(-1, 1) * exp(-eighth_time * 85) * 0.012
        edge = min(1.0, t / 0.07, (duration - t) / 0.07)
        samples.append((pad + pluck + low + hat) * max(0, edge))
    write("BGM_SignalLoop.wav", samples, 0.68)


def interact():
    samples = []
    for index in range(round(0.27 * RATE)):
        t = index / RATE
        tone = 659.25 if t < 0.11 else 987.77
        age = t if t < 0.11 else t - 0.11
        envelope = (1 - exp(-age * 110)) * exp(-age * 23)
        samples.append((sin(2 * pi * tone * t) + 0.25 * sin(4 * pi * tone * t)) * envelope)
    write("SFX_Interaction.wav", samples)


def hurt():
    rng = Random(31)
    samples = []
    for index in range(round(0.38 * RATE)):
        t = index / RATE
        envelope = (1 - exp(-t * 100)) * exp(-t * 9)
        tone = sin(2 * pi * (155 * t - 110 * t * t))
        grit = rng.uniform(-1, 1) * 0.35
        samples.append((tone + grit) * envelope)
    write("SFX_Hurt.wav", samples)


def defeat():
    samples = []
    for index in range(round(0.52 * RATE)):
        t = index / RATE
        envelope = (1 - exp(-t * 100)) * exp(-t * 7)
        glide = sin(2 * pi * (740 * t - 560 * t * t))
        shimmer = 0.24 * sin(2 * pi * 1480 * t)
        samples.append((glide + shimmer) * envelope)
    write("SFX_Defeat.wav", samples)


if __name__ == "__main__":
    bgm()
    interact()
    hurt()
    defeat()
