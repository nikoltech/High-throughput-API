# Docker

### Обычный запуск
```bash
docker compose up --build
```

### С бенчмарком
```bash
docker compose -f docker-compose.yml -f docker-compose.benchmark.yml up --build
```

### Следить за ходом бенчмарка
```bash
docker compose logs -f benchmarks
```

### Открыть результаты в браузере (Windows)
```powershell
start BenchmarkResults\results\HApi.Benchmarks.OrdersBenchmark-report.html
```

### Остановка
```bash
docker compose down
```

### Остановка + удаление данных
```bash
docker compose down -v
```

---

# Database

| Параметр | Значение |
|---|---|
| `SeedOnStartup: true` | заполнить 50 000 записей при старте (пропустит если данные уже есть) |
| `PurgeOnStartup: true` | очистить таблицу перед стартом |

Оба флага в `src/HApi/appsettings.json` → секция `Database`.

---

# Endpoints

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/health` | статус сервиса |
| `GET` | `/orders` | список (EF, output cache) |
| `GET` | `/orders/fast` | список (Dapper) |
| `GET` | `/orders/stats` | агрегация (in-memory cache) |
| `GET` | `/orders/completed?customerId=1` | завершённые заказы клиента |
| `GET` | `/orders/baseline` | без оптимизаций, точка отсчёта для бенчмарков |
| `POST` | `/orders` | создать заказ |
