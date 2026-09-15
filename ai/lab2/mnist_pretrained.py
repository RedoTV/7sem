# -*- coding: utf-8 -*-
"""
ЛАБОРАТОРНАЯ РАБОТА 2
Распознавание рукописных цифр MNIST готовой предобученной CNN.
Никакого обучения — только инференс (forward pass) скачанных весов.

Запуск:  ./venvs/tensorflow/bin/python lab2/mnist_pretrained.py
"""
import os

# Глушим информационные логи TensorFlow ДО импорта (уровень 3 = только ошибки)
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"

import warnings
warnings.filterwarnings("ignore")  # убираем шумные предупреждения Keras

import urllib.request

import matplotlib.pyplot as plt
import numpy as np
import tensorflow as tf

# Пути считаем от папки скрипта — работает при запуске из любого места
BASE_DIR   = os.path.dirname(os.path.abspath(__file__))
MODEL_URL  = "https://huggingface.co/osanseviero/keras-conv-mnist/resolve/main/tf_model.h5"
MODEL_PATH = os.path.join(BASE_DIR, "model", "mnist_pretrained.h5")

np.random.seed(42)  # чтобы примеры на картинках были одинаковыми при каждом запуске


def section(title):
    """Красивый разделитель секций в консоли."""
    print(f"\n{'=' * 64}\n  {title}\n{'=' * 64}")


try:
    # ------------------------------------------------------------------
    # ШАГ 1. ГОТОВАЯ МОДЕЛЬ
    # ------------------------------------------------------------------
    section("[1/5] Загрузка предобученной модели")
    if not os.path.exists(MODEL_PATH):
        os.makedirs(os.path.dirname(MODEL_PATH), exist_ok=True)
        print("  Скачиваю веса с Hugging Face (один раз, дальше кэш)...")
        urllib.request.urlretrieve(MODEL_URL, MODEL_PATH)
    print(f"  Веса: {os.path.relpath(MODEL_PATH, BASE_DIR)} "
          f"({os.path.getsize(MODEL_PATH) // 1024} КБ)")

    # compile=False — не восстанавливать состояние оптимизатора:
    # мы НЕ обучаем, оптимизатор не нужен, заодно исчезают предупреждения
    model = tf.keras.models.load_model(MODEL_PATH, compile=False)

    # ------------------------------------------------------------------
    # ШАГ 2. АРХИТЕКТУРА — что за слои внутри и зачем они
    # ------------------------------------------------------------------
    section("[2/5] Архитектура сети (34 826 обученных параметров)")
    model.summary(line_length=64)

    print("""
  Что делает каждый слой:
   Conv2D(32, 3x3)     — находит простые признаки: края, штрихи, углы
   MaxPooling2D(2x2)   — сжимает карту признаков вдвое, оставляя сильнейшие
   Conv2D(64, 3x3)     — комбинирует простые признаки в сложные (петли, крючки)
   MaxPooling2D(2x2)   — снова сжатие 13x13 -> 5x5
   Flatten             — разворачивает 5x5x64 = 1600 чисел в один вектор
   Dropout(0.5)        — на инференсе ничего не делает (защита от переобучения при обучении)
   Dense(10, softmax)  — 10 цифр; softmax превращает выход в вероятности (сумма = 1)""")

    # ------------------------------------------------------------------
    # ШАГ 3. ДАННЫЕ
    # ------------------------------------------------------------------
    section("[3/5] Тестовые данные MNIST")
    (_, _), (x_test, y_test) = tf.keras.datasets.mnist.load_data()
    x_test = x_test.astype("float32") / 255.0   # пиксели 0..255 -> 0..1, как при обучении
    print(f"  Картинок: {len(x_test)} | размер: 28x28 | классов: 10 (цифры 0-9)")

    # ------------------------------------------------------------------
    # ШАГ 4. ПРЕДСКАЗАНИЕ И МЕТРИКИ
    # ------------------------------------------------------------------
    section("[4/5] Инференс на всём тесте (10 000 картинок)")
    proba = model.predict(x_test[..., None], verbose=0)  #[..., None] = добавить канал (28,28)->(28,28,1)
    pred = proba.argmax(axis=1)                          # цифра с максимальной вероятностью
    conf = proba.max(axis=1)                             # эта максимальная вероятность = уверенность
    correct = pred == y_test

    print(f"  Accuracy: {correct.mean() * 100:.2f}%  "
          f"({correct.sum()} верных из {len(y_test)})")
    print(f"  Средняя уверенность: {conf.mean() * 100:.1f}%  "
          f"(на верных: {conf[correct].mean() * 100:.1f}%, "
          f"на ошибках: {conf[~correct].mean() * 100:.1f}%)")

    print("\n  Точность по каждой цифре:")
    for d in range(10):
        m = y_test == d
        bar = "█" * int(round(correct[m].mean() * 30))
        print(f"   {d}: {correct[m].mean() * 100:5.1f}% |{bar:<30}| ({m.sum()} шт.)")

    # ------------------------------------------------------------------
    # ШАГ 5. ГРАФИКИ — открываются в интерактивных окнах
    # ------------------------------------------------------------------
    section("[5/5] Открытие графиков (закрой окна, чтобы завершить)")

    # 5.1 Примеры предсказаний: картинка + предсказание + уверенность
    fig, axes = plt.subplots(2, 5, figsize=(13, 5.5))
    fig.suptitle("Предсказания модели (зелёный — верно, красный — ошибка)",
                 fontsize=13, fontweight="bold")
    for ax, idx in zip(axes.ravel(), np.random.choice(len(x_test), 10, replace=False)):
        ax.imshow(x_test[idx], cmap="gray")
        ok = correct[idx]
        ax.set_title(f"Пред: {pred[idx]}  ({conf[idx] * 100:.0f}%)\n"
                     f"Верно: {y_test[idx]}",
                     color="green" if ok else "red", fontsize=10)
        ax.axis("off")
    fig.tight_layout()

    # 5.2 Матрица ошибок: строки = верная цифра, столбцы = предсказанная
    cm = np.zeros((10, 10), dtype=int)
    for t, p in zip(y_test, pred):
        cm[t, p] += 1
    fig, ax = plt.subplots(figsize=(8.5, 7))
    im = ax.imshow(cm, cmap="Blues")
    ax.set_xticks(range(10)); ax.set_yticks(range(10))
    ax.set_xlabel("Предсказанная цифра"); ax.set_ylabel("Настоящая цифра")
    ax.set_title("Матрица ошибок (диагональ = верные ответы)", fontweight="bold")
    for i in range(10):
        for j in range(10):
            if cm[i, j]:
                ax.text(j, i, cm[i, j], ha="center", va="center", fontsize=8,
                        color="white" if cm[i, j] > cm.max() / 2 else "black")
    fig.colorbar(im, ax=ax, label="Число картинок")
    fig.tight_layout()

    # 5.3 Уверенность модели: верные ответы vs ошибки
    fig, ax = plt.subplots(figsize=(9, 5))
    bins = np.linspace(0, 1, 41)
    ax.hist(conf[correct], bins=bins, alpha=0.7, color="tab:green",
            label=f"Верные ({correct.sum()})", density=True)
    ax.hist(conf[~correct], bins=bins, alpha=0.7, color="tab:red",
            label=f"Ошибки ({(~correct).sum()})", density=True)
    ax.set_xlabel("Уверенность модели (максимальная вероятность)")
    ax.set_ylabel("Доля примеров")
    ax.set_title("Модель почти всегда уверена на верных ответах", fontweight="bold")
    ax.legend(); fig.tight_layout()

    # 5.4 Точность по классам
    accs = [correct[y_test == d].mean() * 100 for d in range(10)]
    fig, ax = plt.subplots(figsize=(9, 5))
    bars = ax.bar(range(10), accs, color="tab:blue")
    ax.axhline(correct.mean() * 100, color="red", ls="--",
               label=f"Средняя: {correct.mean() * 100:.2f}%")
    for b, a in zip(bars, accs):  # подпись значения над каждым столбцом
        ax.text(b.get_x() + b.get_width() / 2, a + 0.2, f"{a:.1f}%",
                ha="center", fontsize=9)
    ax.set_ylim(97, 100)
    ax.set_xticks(range(10)); ax.set_xlabel("Цифра"); ax.set_ylabel("Accuracy, %")
    ax.set_title("Точность по каждой цифре", fontweight="bold")
    ax.legend(); fig.tight_layout()

    # Показываем все окна сразу; скрипт ждёт, пока их не закроют
    plt.show()

    print("\nГотово: модель нигде не обучалась, только скачанные веса + предсказание.")

except Exception as e:
    print(f"\nОШИБКА: {type(e).__name__}: {e}")
    raise SystemExit(1)
