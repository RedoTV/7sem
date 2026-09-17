// Lab7: Observer — издатель (обучение нейросети) оповещает подписчиков о событиях

#include <iostream>
#include <vector>

// общий интерфейс подписчиков
class Observer {
public:
    virtual void update(double loss) = 0;
    virtual ~Observer() {}
};

// издатель — процесс обучения нейросети
class Training {
    std::vector<Observer*> observers;
public:
    void attach(Observer* o) {
        observers.push_back(o);
    }

    void detach(Observer* o) {
        for (size_t i = 0; i < observers.size(); i++) {
            if (observers[i] == o) {
                observers.erase(observers.begin() + i);
                break;
            }
        }
    }

    // разослать событие всем подписчикам
    void notify(double loss) {
        for (size_t i = 0; i < observers.size(); i++) {
            observers[i]->update(loss);
        }
    }

    // симуляция обучения: loss понемногу уменьшается
    void run(int epochs) {
        double loss = 2.0;
        for (int epoch = 1; epoch <= epochs; epoch++) {
            loss = loss * 0.8;
            std::cout << "Эпоха " << epoch << ", loss = " << loss << "\n";
            notify(loss);
        }
    }
};

// подписчик 1: логирует каждую эпоху
class Logger : public Observer {
public:
    void update(double loss) {
        (void)loss; // логгеру loss не нужен
        std::cout << "    [лог] эпоха завершена\n";
    }
};

// подписчик 2: реагирует только на улучшение результата
class Checkpoint : public Observer {
    double best;
public:
    Checkpoint() {
        best = 1000000;
    }

    void update(double loss) {
        if (loss < best) {
            best = loss;
            std::cout << "    [чекпойнт] новый лучший loss, сохраняю модель\n";
        }
    }
};

int main() {
    Training training;
    Logger logger;
    Checkpoint checkpoint;

    training.attach(&logger);
    training.attach(&checkpoint);

    std::cout << "Обучение с двумя подписчиками:\n";
    training.run(6);

    training.detach(&logger);

    std::cout << "\nПродолжаем без логгера:\n";
    training.run(3);

    return 0;
}
