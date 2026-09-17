# -*- coding: utf-8 -*-
"""
Лабораторная работа 2. Transfer Learning.
Готовая сеть MobileNetV2 (веса ImageNet, как в лабе 1) дообучается на MNIST:
отсекается исходная голова на 1000 классов, вместо неё добавляется своя
на 10 выходов, база замораживается, дообучается только новая голова.

Запуск:  ./venvs/tensorflow/bin/python lab2/mobilenet_transfer.py
Первый прогон обучает голову (около 7-10 минут на CPU) и сохраняет веса в model/.
Следующие прогоны загружают готовые веса и работают около минуты.
"""
import os

os.environ["TF_CPP_MIN_LOG_LEVEL"] = "3"

import warnings
warnings.filterwarnings("ignore")

import matplotlib.pyplot as plt
import numpy as np
import tensorflow as tf
import time

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
WEIGHTS_PATH = os.path.join(BASE_DIR, "model", "mobilenet_mnist_head.weights.h5")

IMG_SIZE = 96
BATCH = 128
EPOCHS = 2


def section(title):
    print("\n" + "=" * 60)
    print("  " + title)
    print("=" * 60)


def make_dataset(images, labels, shuffle=False):
    # Препроцессинг из лабы 1: серое 28x28 -> RGB 96x96 -> нормализация [-1, 1].
    ds = tf.data.Dataset.from_tensor_slices((images, labels))
    if shuffle:
        ds = ds.shuffle(buffer_size=5000)

    def _prep(img, lbl):
        img = tf.cast(img, tf.float32)
        img = tf.expand_dims(img, -1)
        img = tf.repeat(img, 3, -1)
        img = tf.image.resize(img, (IMG_SIZE, IMG_SIZE))
        img = tf.keras.applications.mobilenet_v2.preprocess_input(img)
        return img, tf.cast(lbl, tf.int32)

    return ds.map(_prep, num_parallel_calls=tf.data.AUTOTUNE).batch(BATCH).prefetch(tf.data.AUTOTUNE)


try:
    section("Шаг 1 из 6. Данные MNIST")
    (x_train, y_train), (x_test, y_test) = tf.keras.datasets.mnist.load_data()
    x_val, y_val = x_train[54000:], y_train[54000:]
    x_train, y_train = x_train[:54000], y_train[:54000]
    print(f"  Обучение: {len(x_train)}, валидация: {len(x_val)}, тест: {len(x_test)}")

    train_ds = make_dataset(x_train, y_train, shuffle=True)
    val_ds = make_dataset(x_val, y_val)
    test_ds = make_dataset(x_test, y_test)

    section("Шаг 2 из 6. Модель: MobileNetV2 + новая голова на 10 классов")
    base_model = tf.keras.applications.MobileNetV2(
        input_shape=(IMG_SIZE, IMG_SIZE, 3),
        include_top=False,
        weights="imagenet",
        name="mobilenet_base",
    )
    base_model.trainable = False

    model = tf.keras.Sequential([
        base_model,
        tf.keras.layers.GlobalAveragePooling2D(name="gap"),
        tf.keras.layers.Dense(128, activation="relu", name="head_dense"),
        tf.keras.layers.Dropout(0.2, name="head_dropout"),
        tf.keras.layers.Dense(10, activation="softmax", name="predictions"),
    ], name="mnist_transfer")
    # В Keras 3.15 аргумент loss_weights в compile идёт раньше metrics,
    # поэтому metrics передаётся только по имени.
    model.compile(optimizer="adam", loss="sparse_categorical_crossentropy", metrics=["accuracy"])

    head = sum(np.prod(w.shape) for w in model.trainable_weights)
    frozen = sum(np.prod(w.shape) for w in model.non_trainable_weights)
    print(f"  База MobileNetV2: {len(base_model.layers)} слоёв, {frozen / 1e6:.1f}M параметров (заморожены)")
    print(f"  Новая голова: {head / 1e6:.2f}M параметров (обучаются)")

    if os.path.exists(WEIGHTS_PATH):
        section("Шаг 3 из 6. Загрузка сохранённых весов")
        print(f"  Файл: {os.path.relpath(WEIGHTS_PATH, BASE_DIR)}")
        print("  Чтобы обучить заново, удалите этот файл и запустите скрипт снова.")
        model.load_weights(WEIGHTS_PATH)
        history = None
    else:
        section(f"Шаг 3 из 6. Дообучение головы: {EPOCHS} эпохи")
        t0 = time.time()
        history = model.fit(train_ds, validation_data=val_ds, epochs=EPOCHS, verbose=1)
        print(f"  Время обучения: {(time.time() - t0) / 60:.1f} мин")
        os.makedirs(os.path.dirname(WEIGHTS_PATH), exist_ok=True)
        model.save_weights(WEIGHTS_PATH)
        print(f"  Веса сохранены: {os.path.relpath(WEIGHTS_PATH, BASE_DIR)}")

    section("Шаг 4 из 6. Оценка на тесте (10 000 изображений)")
    test_loss, test_acc = model.evaluate(test_ds, verbose=0)
    proba = model.predict(test_ds, verbose=0)
    pred = proba.argmax(axis=1)
    conf = proba.max(axis=1)
    correct = pred == y_test

    print(f"  Accuracy: {test_acc * 100:.2f}% (zero-shot из лабы 1: около 13.5%)")
    print(f"  Loss: {test_loss:.4f}")
    print(f"  Средняя уверенность: {conf.mean() * 100:.1f}% "
          f"(верные ответы: {conf[correct].mean() * 100:.1f}%, "
          f"ошибки: {conf[~correct].mean() * 100:.1f}%)")

    print("\n  Точность по классам:")
    for d in range(10):
        m = y_test == d
        bar = "#" * int(round(correct[m].mean() * 30))
        print(f"   {d}: {correct[m].mean() * 100:5.1f}% |{bar:<30}| (n={m.sum()})")

    section("Шаг 5 из 6. Графики")
    print("  Окна откроются после завершения расчётов.")
    cm = np.zeros((10, 10), dtype=int)
    for t, p in zip(y_test, pred):
        cm[t, p] += 1

    if history is not None:
        h = history.history
        ep = range(1, EPOCHS + 1)
        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 4.5))
        fig.suptitle("Кривые обучения", fontweight="bold")
        ax1.plot(ep, h["loss"], "b-o", label="Обучение")
        ax1.plot(ep, h["val_loss"], "r--o", label="Валидация")
        ax1.set_title("Loss")
        ax1.set_xlabel("Эпоха")
        ax1.legend()
        ax1.grid(alpha=0.3)
        ax2.plot(ep, h["accuracy"], "g-o", label="Обучение")
        ax2.plot(ep, h["val_accuracy"], "m--o", label="Валидация")
        ax2.set_title("Accuracy")
        ax2.set_xlabel("Эпоха")
        ax2.legend()
        ax2.grid(alpha=0.3)
        fig.tight_layout()

    fig, ax = plt.subplots(figsize=(8.5, 7))
    im = ax.imshow(cm, cmap="Blues")
    ax.set_xticks(range(10))
    ax.set_yticks(range(10))
    ax.set_xlabel("Предсказанный класс")
    ax.set_ylabel("Истинный класс")
    ax.set_title(f"Матрица ошибок (accuracy {test_acc * 100:.2f}%)", fontweight="bold")
    for i in range(10):
        for j in range(10):
            if cm[i, j]:
                ax.text(j, i, cm[i, j], ha="center", va="center", fontsize=8,
                        color="white" if cm[i, j] > cm.max() / 2 else "black")
    fig.colorbar(im, ax=ax, label="Количество изображений")
    fig.tight_layout()

    fig, axes = plt.subplots(2, 5, figsize=(13, 5.5))
    fig.suptitle("Примеры предсказаний (зелёный — верно, красный — ошибка)", fontweight="bold")
    rng = np.random.default_rng(42)
    for ax_i, idx in zip(axes.ravel(), rng.choice(len(x_test), 10, replace=False)):
        ok = correct[idx]
        ax_i.imshow(x_test[idx], cmap="gray")
        ax_i.set_title(f"Предсказано: {pred[idx]} ({conf[idx] * 100:.0f}%)\nИстинный класс: {y_test[idx]}",
                       color="green" if ok else "red", fontsize=10)
        ax_i.axis("off")
    fig.tight_layout()

    accs = [correct[y_test == d].mean() * 100 for d in range(10)]
    fig, ax = plt.subplots(figsize=(9, 5))
    bars = ax.bar(range(10), accs, color="tab:blue")
    ax.axhline(test_acc * 100, color="red", ls="--", label=f"Среднее: {test_acc * 100:.2f}%")
    for b, a in zip(bars, accs):
        ax.text(b.get_x() + b.get_width() / 2, a + 0.15, f"{a:.1f}%", ha="center", fontsize=9)
    ax.set_ylim(max(0, min(accs) - 3), 100)
    ax.set_xticks(range(10))
    ax.set_xlabel("Класс")
    ax.set_ylabel("Accuracy, %")
    ax.set_title("Точность по классам", fontweight="bold")
    ax.legend()
    fig.tight_layout()

    plt.show()

    print(f"\nИтог: zero-shot из лабы 1 давал около 13.5%, после дообучения — {test_acc * 100:.2f}%.")

except KeyboardInterrupt:
    print("\nВыполнение прервано пользователем (Ctrl+C).")
    raise SystemExit(130)

except Exception as e:
    print(f"\nОшибка: {type(e).__name__}: {e}")
    raise SystemExit(1)
