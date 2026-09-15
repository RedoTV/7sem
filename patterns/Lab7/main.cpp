// Лаба 7. Observer: издатель TrainingRun рассылает метрики эпох подписчикам.
// Сборка: g++ -std=c++17 -Wall -Wextra -o lab7_observer main.cpp

#include <cmath>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <mutex>
#include <string>
#include <utility>
#include <vector>

namespace lab7 {

struct EpochMetrics {
    int epoch = 0;
    int total_epochs = 0;
    double train_loss = 0.0;
    double val_accuracy = 0.0;
    double learning_rate = 0.0;
};

class TrainingRun;

class TrainingObserver {
public:
    virtual ~TrainingObserver() = default;
    virtual void OnEpoch(const EpochMetrics& metrics) = 0;
    virtual void OnTrainingFinished(const EpochMetrics& last) { (void)last; }
};

// издатель: знает только абстрактный наблюдатель, о конкретных классах — ничего
class TrainingRun {
public:
    explicit TrainingRun(std::string model_name) : model_name_(std::move(model_name)) {}

    void Attach(TrainingObserver* observer) {
        std::lock_guard<std::mutex> lock(observers_mutex_);
        observers_.push_back(observer);
        std::cout << "[ИЗДАТЕЛЬ] подписан наблюдатель №" << observers_.size() << "\n";
    }

    void Detach(TrainingObserver* observer) {
        std::lock_guard<std::mutex> lock(observers_mutex_);
        for (auto it = observers_.begin(); it != observers_.end(); ++it) {
            if (*it == observer) {
                observers_.erase(it);
                std::cout << "[ИЗДАТЕЛЬ] наблюдатель отписан\n";
                return;
            }
        }
    }

    void Run(int total_epochs) {
        EpochMetrics last;
        for (int epoch = 1; epoch <= total_epochs && !stop_requested_; ++epoch) {
            last = SimulateEpoch(epoch, total_epochs);
            NotifyEpoch(last);
        }
        std::cout << "[ИЗДАТЕЛЬ] обучение остановлено на эпохе " << last.epoch
                  << " из " << last.total_epochs << "\n";
        NotifyFinished(last);
    }

    void Stop() { stop_requested_ = true; }

private:
    // игровые метрики: качество выходит на плато после ~9-й эпохи
    EpochMetrics SimulateEpoch(int epoch, int total_epochs) const {
        static const double kValAccuracy[] = {
            0.620, 0.710, 0.790, 0.840, 0.870, 0.890,
            0.900, 0.904, 0.9055, 0.9060, 0.9062, 0.9063};
        const int kTableSize = static_cast<int>(sizeof(kValAccuracy) / sizeof(kValAccuracy[0]));

        EpochMetrics m;
        m.epoch = epoch;
        m.total_epochs = total_epochs;
        m.val_accuracy = (epoch <= kTableSize) ? kValAccuracy[epoch - 1] : kValAccuracy[kTableSize - 1];
        m.train_loss = 0.35 + 1.8 * std::exp(-(epoch - 1) / 3.5);
        m.learning_rate = 0.001 * std::pow(0.9, epoch - 1);
        return m;
    }

    void NotifyEpoch(const EpochMetrics& m) {
        // копия списка: наблюдатель может отписаться прямо во время оповещения
        std::vector<TrainingObserver*> snapshot;
        {
            std::lock_guard<std::mutex> lock(observers_mutex_);
            snapshot = observers_;
        }
        for (TrainingObserver* obs : snapshot) obs->OnEpoch(m);
    }

    void NotifyFinished(const EpochMetrics& m) {
        std::vector<TrainingObserver*> snapshot;
        {
            std::lock_guard<std::mutex> lock(observers_mutex_);
            snapshot = observers_;
        }
        for (TrainingObserver* obs : snapshot) obs->OnTrainingFinished(m);
    }

    std::string model_name_;
    std::vector<TrainingObserver*> observers_;
    std::mutex observers_mutex_;
    bool stop_requested_ = false;
};

// печатает метрики эпохи
class ConsoleLoggerObserver : public TrainingObserver {
public:
    void OnEpoch(const EpochMetrics& m) override {
        std::cout << "   [ЛОГЕР    ] эпоха " << std::setw(2) << m.epoch << "/" << m.total_epochs
                  << " | loss " << std::fixed << std::setprecision(4) << m.train_loss
                  << " | val_acc " << m.val_accuracy
                  << " | lr " << std::setprecision(6) << m.learning_rate << "\n";
    }
};

// при плато качества останавливает обучение через run.Stop()
class EarlyStoppingObserver : public TrainingObserver {
public:
    EarlyStoppingObserver(TrainingRun& run, int patience, double min_delta)
        : run_(run), patience_(patience), min_delta_(min_delta) {}

    void OnEpoch(const EpochMetrics& m) override {
        if (m.val_accuracy > best_accuracy_ + min_delta_) {
            best_accuracy_ = m.val_accuracy;
            bad_epochs_ = 0;
            std::cout << "   [EARLYSTOP] новое лучшее качество: " << std::fixed
                      << std::setprecision(4) << best_accuracy_ << "\n";
        } else {
            ++bad_epochs_;
            std::cout << "   [EARLYSTOP] улучшений нет: " << bad_epochs_ << "/" << patience_;
            if (bad_epochs_ >= patience_) {
                std::cout << " -> стоп, плато достигнуто";
                run_.Stop();
            }
            std::cout << "\n";
        }
    }

private:
    TrainingRun& run_;
    int patience_;
    double min_delta_;
    double best_accuracy_ = 0.0;
    int bad_epochs_ = 0;
};

// сохраняет лучший чекпойнт в json
class CheckpointSaverObserver : public TrainingObserver {
public:
    explicit CheckpointSaverObserver(std::string path) : path_(std::move(path)) {}

    void OnEpoch(const EpochMetrics& m) override {
        if (m.val_accuracy > best_accuracy_) {
            best_accuracy_ = m.val_accuracy;

            std::ofstream out(path_);
            out << "{\n"
                << "  \"epoch\": " << m.epoch << ",\n"
                << "  \"val_accuracy\": " << m.val_accuracy << ",\n"
                << "  \"train_loss\": " << m.train_loss << ",\n"
                << "  \"learning_rate\": " << m.learning_rate << "\n"
                << "}\n";

            std::cout << "   [ЧЕКПОИНТ ] сохранён лучший: эпоха " << m.epoch
                      << ", val_acc " << std::fixed << std::setprecision(4)
                      << m.val_accuracy << " -> " << path_ << "\n";
        }
    }

private:
    std::string path_;
    double best_accuracy_ = 0.0;
};

// считает GPU-минуты и деньги
class GpuCostObserver : public TrainingObserver {
public:
    GpuCostObserver(double usd_per_gpu_hour, double seconds_per_epoch)
        : usd_per_hour_(usd_per_gpu_hour), seconds_per_epoch_(seconds_per_epoch) {}

    void OnEpoch(const EpochMetrics& m) override {
        (void)m;
        ++epochs_done_;
    }

    void OnTrainingFinished(const EpochMetrics&) override {
        const double hours = epochs_done_ * seconds_per_epoch_ / 3600.0;
        std::cout << "   [СЧЁТЧИК  ] эпох: " << epochs_done_
                  << ", GPU-время: " << hours * 60.0 << " мин, стоимость: $"
                  << std::fixed << std::setprecision(2) << hours * usd_per_hour_ << "\n";
    }

private:
    double usd_per_hour_;
    double seconds_per_epoch_;
    int epochs_done_ = 0;
};

// шумный отладчик — его отцепляем в main, демонстрация Detach
class GradientDebugObserver : public TrainingObserver {
public:
    void OnEpoch(const EpochMetrics& m) override {
        const double grad_norm = 0.5 * std::exp(-(m.epoch - 1) / 4.0);
        std::cout << "   [DEBUG    ] ||градиент|| = " << std::fixed
                  << std::setprecision(6) << grad_norm << ", всплесков не найдено\n";
    }
};

} // namespace lab7

int main() {
    using namespace lab7;

    TrainingRun run("resnet-50-cats-vs-dogs");

    ConsoleLoggerObserver logger;
    CheckpointSaverObserver saver("best_checkpoint.json");
    GpuCostObserver cost(2.30, 45.0);
    GradientDebugObserver debug;

    run.Attach(&logger);
    run.Attach(&saver);
    run.Attach(&cost);
    run.Attach(&debug);

    std::cout << "\n=== Фаза 1: пробный прогон на 4 эпохи (все подписчики) ===\n";
    run.Run(4);

    std::cout << "\n=== Отписываем DEBUG-наблюдатель — слишком много шума ===\n";
    run.Detach(&debug);

    std::cout << "\n=== Фаза 2: обучение до 20 эпох с early stopping (patience=3) ===\n";
    EarlyStoppingObserver early(run, 3, 0.002);
    run.Attach(&early);
    run.Run(20);

    std::cout << "\nГотово. Лучший чекпойнт — в best_checkpoint.json\n";
    return 0;
}
