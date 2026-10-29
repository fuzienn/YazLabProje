"""TensorBoard kayitlarindan egitim grafiklerini uretir.

Kullanim: .venv/bin/python plot_training.py [run_id]
Cikti: ../Docs/ml/*.png ve ../Docs/ml/training_summary.md
"""
import glob
import os
import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from tensorboard.backend.event_processing.event_accumulator import EventAccumulator

RUN_ID = sys.argv[1] if len(sys.argv) > 1 else "rat_stealth"
HERE = os.path.dirname(os.path.abspath(__file__))
EVENTS = os.path.join(HERE, "results", RUN_ID, "RatStealth")
OUT = os.path.join(HERE, "..", "Docs", "ml")

CHARTS = [
    ("Stealth/Success", "Basari orani (gorulmeden arkaya ulasma)", "success_rate.png"),
    ("Stealth/Caught", "Yakalanma orani", "caught_rate.png"),
    ("Environment/Cumulative Reward", "Bolum basina toplam odul", "cumulative_reward.png"),
    ("Environment/Episode Length", "Bolum uzunlugu (adim)", "episode_length.png"),
    ("Policy/Entropy", "Politika entropisi", "entropy.png"),
    ("Losses/Value Loss", "Deger kaybi", "value_loss.png"),
]


def smooth(values, weight=0.8):
    smoothed, last = [], values[0]
    for v in values:
        last = last * weight + (1 - weight) * v
        smoothed.append(last)
    return smoothed


ATTEMPTS = [
    ("rat_stealth_v1", "1. deneme: yonlendirme 0.1", "#9fb3d9"),
    ("rat_stealth_v2", "2. deneme: yonlendirme 0.5, terminal duzeltmesi yok", "#c75b5b"),
    ("rat_stealth", "3. deneme: potansiyel tabanli (terminal sifirlama)", "#d9822b"),
]


def plot_attempts():
    """Uc denemenin odul egrilerini tek grafikte karsilastirir."""
    plt.figure(figsize=(7, 3.6), dpi=120)
    for run, label, color in ATTEMPTS:
        path = os.path.join(HERE, "results", run, "RatStealth")
        if not glob.glob(os.path.join(path, "events.out.tfevents.*")):
            continue
        acc = EventAccumulator(path, size_guidance={"scalars": 0})
        acc.Reload()
        events = acc.Scalars("Environment/Cumulative Reward")
        plt.plot([e.step for e in events], smooth([e.value for e in events]), label=label, color=color, linewidth=1.6)
    plt.axhline(0, color="#888", linewidth=0.8)
    plt.title("Bolum basina toplam odul: uc deneme")
    plt.xlabel("Adim")
    plt.grid(alpha=0.3)
    plt.legend(fontsize=7)
    plt.tight_layout()
    plt.savefig(os.path.join(OUT, "reward_comparison.png"))
    plt.close()


def main():
    files = glob.glob(os.path.join(EVENTS, "events.out.tfevents.*"))
    if not files:
        sys.exit(f"Kayit bulunamadi: {EVENTS}")
    acc = EventAccumulator(EVENTS, size_guidance={"scalars": 0})
    acc.Reload()
    os.makedirs(OUT, exist_ok=True)

    summary = [f"# Egitim Ozeti ({RUN_ID})", ""]
    for tag, title, name in CHARTS:
        if tag not in acc.Tags()["scalars"]:
            continue
        events = acc.Scalars(tag)
        steps = [e.step for e in events]
        values = [e.value for e in events]

        plt.figure(figsize=(7, 3.6), dpi=120)
        plt.plot(steps, values, color="#9fb3d9", linewidth=1, label="ham")
        plt.plot(steps, smooth(values), color="#d9822b", linewidth=2, label="yumusatilmis")
        plt.title(title)
        plt.xlabel("Adim")
        plt.grid(alpha=0.3)
        plt.legend()
        plt.tight_layout()
        plt.savefig(os.path.join(OUT, name))
        plt.close()

        first = sum(values[:3]) / min(3, len(values))
        last = sum(values[-3:]) / min(3, len(values))
        summary.append(f"- {title}: basta {first:.3f}, sonda {last:.3f} ({steps[-1]:,} adim)")

    plot_attempts()
    with open(os.path.join(OUT, "training_summary.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(summary) + "\n")
    print("\n".join(summary))


if __name__ == "__main__":
    main()
