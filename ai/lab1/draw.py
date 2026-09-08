"""Окно для рисования собственной цифры: python draw.py."""

import tkinter as tk
from tkinter import messagebox

from PIL import Image, ImageDraw
from main import load_model, predict

SIZE = 280
WIDTH = 18


def main():
    # Загружаем сеть один раз, а не после каждого нажатия кнопки.
    try:
        model = load_model()
    except FileNotFoundError as error:
        print(error)
        return

    root = tk.Tk()
    root.title("MNIST — нарисуйте одну цифру")
    canvas = tk.Canvas(root, width=SIZE, height=SIZE, bg="white",
                       highlightthickness=0)
    canvas.pack(padx=10, pady=10)
    result = tk.StringVar(value="Нарисуйте цифру 0–9 левой кнопкой мыши")
    tk.Label(root, textvariable=result).pack(padx=10, pady=5)

    # Canvas нужен для показа, PIL-картинка — для передачи нейросети.
    image = Image.new("RGB", (SIZE, SIZE), "white")
    painter = ImageDraw.Draw(image)
    previous = None

    def paint(event):
        nonlocal previous
        x, y = event.x, event.y
        radius = WIDTH // 2
        if previous is not None:
            coords = (*previous, x, y)
            canvas.create_line(*coords, fill="black", width=WIDTH)
            painter.line(coords, fill="black", width=WIDTH)
        # Круги делают концы штриха гладкими; одиночный клик тоже рисует.
        box = (x - radius, y - radius, x + radius, y + radius)
        canvas.create_oval(*box, fill="black", outline="black")
        painter.ellipse(box, fill="black")
        previous = (x, y)

    def release(event):
        nonlocal previous
        previous = None

    def clear():
        nonlocal previous
        canvas.delete("all")
        painter.rectangle((0, 0, SIZE, SIZE), fill="white")
        previous = None
        result.set("Нарисуйте новую цифру")

    def recognize():
        try:
            digit, confidence = predict(model, image)
            result.set(f"Цифра: {digit}; softmax: {confidence:.1%}")
        except ValueError as error:
            messagebox.showwarning("Нет цифры", str(error))

    canvas.bind("<Button-1>", paint)
    canvas.bind("<B1-Motion>", paint)
    canvas.bind("<ButtonRelease-1>", release)
    tk.Button(root, text="Распознать", command=recognize).pack(pady=5)
    tk.Button(root, text="Очистить", command=clear).pack(pady=5)
    root.mainloop()


if __name__ == "__main__":
    main()
