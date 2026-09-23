// Lab7: Observer — обобщённый каркас Subject<TEvent>/Observer<TEvent> на шаблонах C++.
// Каркас переиспользуется для разных событий: метрики обучения и котировки.
// Компиляция: g++ -std=c++17 -Wall -Wextra -o lab7 main.cpp && ./lab7

#include <iostream>
#include <string>
#include <vector>

// ================== обобщённый каркас паттерна ==================

// подписчик: реагирует на событие типа TEvent (аналог interface IObserver<T>)
template <typename TEvent>
class Observer {
public:
    virtual ~Observer() = default;
    virtual void update(const TEvent& event) = 0;
};

// издатель: хранит подписчиков и рассылает им события
template <typename TEvent>
class Subject {
public:
    virtual ~Subject() = default;

    void attach(Observer<TEvent>* o) {
        observers_.push_back(o);
    }

    void detach(Observer<TEvent>* o) {
        for (std::size_t i = 0; i < observers_.size(); ++i) {
            if (observers_[i] == o) {
                observers_.erase(observers_.begin() + i);
                return;
            }
        }
    }

    void notify(const TEvent& event) {
        for (Observer<TEvent>* o : observers_) {
            o->update(event);
        }
    }

private:
    std::vector<Observer<TEvent>*> observers_; // не владеет объектами: ими владеет main
};

// ================== сценарий 1: обучение нейросети ==================

struct EpochEvent {
    int epoch;
    int totalEpochs;
    double loss;
    double accuracy;
};

// издатель: наследует каркас для своего типа события
class TrainingRun : public Subject<EpochEvent> {
public:
    explicit TrainingRun(int totalEpochs) : totalEpochs_(totalEpochs) {}

    void run() {
        double loss = 2.0;
        double accuracy = 0.50;
        for (int epoch = 1; epoch <= totalEpochs_ && !stopped_; ++epoch) {
            if (epoch <= 5) {
                loss *= 0.70; // сначала модель учится
            }                // после 5-й эпохи — плато
            accuracy += (0.95 - accuracy) * 0.3;

            std::cout << "[модель] эпоха " << epoch << "/" << totalEpochs_
                      << ", loss = " << loss << ", accuracy = " << accuracy << "\n";
            notify(EpochEvent{epoch, totalEpochs_, loss, accuracy});
        }
    }

    // подписчик может повлиять на издателя через этот метод
    void stop() { stopped_ = true; }

private:
    int totalEpochs_;
    bool stopped_ = false;
};

// подписчик 1: пишет всё в журнал
class MetricsLogger : public Observer<EpochEvent> {
public:
    void update(const EpochEvent& e) override {
        std::cout << "   [логгер] эпоха " << e.epoch << " зафиксирована\n";
    }
};

// подписчик 2: при плато останавливает обучение через run.stop()
class EarlyStopping : public Observer<EpochEvent> {
public:
    EarlyStopping(TrainingRun& run, int patience) : run_(run), patience_(patience) {}

    void update(const EpochEvent& e) override {
        if (e.loss < bestLoss_ - 1e-9) {
            bestLoss_ = e.loss;
            badEpochs_ = 0;
        } else {
            ++badEpochs_;
            std::cout << "   [early stopping] без улучшений: " << badEpochs_
                      << "/" << patience_ << "\n";
            if (badEpochs_ >= patience_) {
                std::cout << "   [early stopping] плато — останавливаем обучение\n";
                run_.stop();
            }
        }
    }

private:
    TrainingRun& run_;
    int patience_;
    double bestLoss_ = 1e9;
    int badEpochs_ = 0;
};

// ================== сценарий 2: биржевые котировки ==================
// Тот же каркас Subject<T>/Observer<T>, другой тип события — переиспользование.

struct PriceEvent {
    std::string ticker;
    double price;
};

class MarketFeed : public Subject<PriceEvent> {
public:
    void publish(const PriceEvent& e) { notify(e); }
};

class AlertBot : public Observer<PriceEvent> {
public:
    explicit AlertBot(double threshold) : threshold_(threshold) {}

    void update(const PriceEvent& e) override {
        if (e.price > threshold_) {
            std::cout << "   [алерт] " << e.ticker << " выше " << threshold_ << "!\n";
        }
    }

private:
    double threshold_;
};

// ================== демонстрация ==================

int main() {
    std::cout << "=== Сценарий 1: обучение нейросети ===\n";
    TrainingRun run(10); // до 10 эпох
    MetricsLogger logger;
    EarlyStopping early(run, /*patience=*/3);

    run.attach(&logger);
    run.attach(&early);
    run.run(); // сам остановится на плато благодаря подписчику

    std::cout << "\n=== Сценарий 2: тот же каркас, другое событие ===\n";
    MarketFeed feed;
    AlertBot bot(150.0);

    feed.attach(&bot);
    feed.publish(PriceEvent{"MOON", 148.0}); // тихо
    feed.publish(PriceEvent{"MOON", 172.0}); // алерт

    feed.detach(&bot);
    feed.publish(PriceEvent{"MOON", 999.0}); // подписчиков нет — никого не оповестили

    return 0;
}
