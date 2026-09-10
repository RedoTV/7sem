"""Обучение простой CNN на MNIST и распознавание одной своей цифры."""

import argparse
import os
from pathlib import Path

# Для лабораторной достаточно CPU. Настройки задаются ДО импорта TensorFlow.
os.environ["CUDA_VISIBLE_DEVICES"] = "-1"
os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")
os.environ.setdefault("KERAS_HOME", str(Path(__file__).parent / ".keras"))

import numpy as np
import tensorflow as tf
from PIL import Image, ImageOps

MODEL_PATH = Path(__file__).parent / "lenet_mnist.keras"


def build_model():
    """Вариант LeNet-5: свёртки -> pooling -> полносвязные слои."""
    return tf.keras.Sequential([
        tf.keras.Input(shape=(28, 28, 1)),
        tf.keras.layers.Conv2D(6, (5, 5), padding="same", activation="relu"),
        tf.keras.layers.AveragePooling2D(pool_size=(2, 2)),
        tf.keras.layers.Conv2D(16, (5, 5), activation="relu"),
        tf.keras.layers.AveragePooling2D(pool_size=(2, 2)),
        tf.keras.layers.Flatten(),
        tf.keras.layers.Dense(120, activation="relu"),
        tf.keras.layers.Dense(84, activation="relu"),
        tf.keras.layers.Dense(10, activation="softmax"),
    ], name="lenet_mnist")


def train(epochs):
    tf.keras.utils.set_random_seed(42)
    (x_train, y_train), (x_test, y_test) = tf.keras.datasets.mnist.load_data()

    # (N, 28, 28) -> (N, 28, 28, 1). Значения 0..255 -> 0..1.
    x_train = x_train.astype("float32")[..., np.newaxis] / 255.0
    x_test = x_test.astype("float32")[..., np.newaxis] / 255.0

    model = build_model()
    model.compile(
        optimizer="adam",
        loss="sparse_categorical_crossentropy",
        metrics=["accuracy"],
    )
    model.summary()
    model.fit(x_train, y_train, epochs=epochs, batch_size=64,
              validation_split=0.1, verbose=2)
    loss, accuracy = model.evaluate(x_test, y_test, verbose=0)
    print(f"Точность на 10 000 тестовых изображений: {accuracy:.2%}")
    print(f"Ошибка (loss): {loss:.4f}")
    model.save(MODEL_PATH)
    print(f"Модель сохранена: {MODEL_PATH}")


def prepare_image(image):
    """Превратить картинку с одной цифрой в вход CNN формы (1, 28, 28, 1)."""
    # Прозрачный фон заменяем белым, затем переводим в оттенки серого.
    rgba = image.convert("RGBA")
    background = Image.new("RGBA", rgba.size, "white")
    image = Image.alpha_composite(background, rgba).convert("L")
    pixels = np.asarray(image)
    border = np.concatenate([pixels[0], pixels[-1], pixels[:, 0], pixels[:, -1]])
    # В MNIST цифра светлая, фон чёрный. Светлый фон инвертируем.
    if np.median(border) > 127:
        image = ImageOps.invert(image)

    image = ImageOps.autocontrast(image)
    # Игнорируем слабый шум при поиске границ цифры.
    mask = image.point(lambda value: 255 if value > 50 else 0)
    box = mask.getbbox()
    if box is None:
        raise ValueError("Цифра не найдена: нарисуйте её на однотонном фоне.")
    image = image.crop(box)
    # Сохраняем пропорции; оставляем вокруг цифры поля, как в MNIST.
    image.thumbnail((20, 20), Image.Resampling.LANCZOS)
    canvas = Image.new("L", (28, 28), 0)
    canvas.paste(image, ((28 - image.width) // 2, (28 - image.height) // 2))
    values = np.asarray(canvas, dtype="float32") / 255.0
    return values[np.newaxis, ..., np.newaxis]


def load_model():
    if not MODEL_PATH.exists():
        raise FileNotFoundError("Сначала обучите сеть: python main.py train")
    return tf.keras.models.load_model(MODEL_PATH)


def predict(model, image):
    probabilities = model.predict(prepare_image(image), verbose=0)[0]
    digit = int(np.argmax(probabilities))
    return digit, float(probabilities[digit])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    training = commands.add_parser("train", help="Обучить и сохранить CNN")
    training.add_argument("--epochs", type=int, default=5)
    recognition = commands.add_parser("predict", help="Распознать цифру из картинки")
    recognition.add_argument("image", type=Path)
    args = parser.parse_args()

    try:
        if args.command == "train":
            if args.epochs < 1:
                parser.error("Число эпох должно быть положительным")
            train(args.epochs)
        else:
            model = load_model()
            with Image.open(args.image) as image:
                digit, confidence = predict(model, image)
            print(f"Цифра: {digit}; оценка softmax: {confidence:.1%}")
    except (OSError, ValueError) as error:
        parser.exit(1, f"Ошибка: {error}\n")


if __name__ == "__main__":
    main()
