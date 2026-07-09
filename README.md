# FFSchedule - Автоматизированное рабочее место (АРМ) построения маршрутов и формирования расписаний для пожарной охраны (МЧС)
*Проект разработан в качестве геоинформационной СППР (системы поддержки принятия решений) для логистики МЧС.*

**FFSchedule** - это настольное WPF-приложение, предназначенное для оперативного планирования, оптимизации логистики пожарных частей и автоматизации документооборота. 

Система рассчитывает оптимальное количество пожарной техники на основе рангов пожаров, прокладывает маршруты через сторонние ГИС-сервисы, управляет инфраструктурой пожарных частей в реальном времени и автоматически генерирует служебные документы (Расписания выездов) в формате MS Word.

---

## 🚀 Основной функционал

### 🗺️ Геоинформационная система (ГИС) и Логистика
* **Интеграция с картами (Mapsui):** Отображение слоев OpenStreetMap, управление интерактивными векторными слоями (границы районов, маркеры пожарных частей) из GeoJSON.
* **Матрица дистанций (OSRM Table API):** Автоматическая сортировка всех доступных пожарных частей по реальному времени доезда до места происшествия, а не по прямой.
* **Алгоритм подбора по рангам:** Система автоматически вычисляет, сколько пожарных частей нужно задействовать, суммируя вместимость их техники (`EquipmentCapacity`) до полного покрытия требований выбранного ранга пожара.
* **Поиск и обратное геокодирование (Nominatim API):** Интерактивный поиск адресов с установкой пинов и возможностью кликнуть дважды по карте для распознавания адреса.
* **Многоуровневое кэширование (Память + Диск):** Оптимизация сетевых запросов. Запросы к OSRM и Nominatim сначала проверяются в оперативной памяти, затем в JSON-файлах на диске, бережно расходуя лимиты API.

### 📝 Автоматизация документооборота и Работа с данными
* **Динамическое управление инфраструктурой:** Возможность добавлять и удалять пожарные части прямо через интерфейс карты с мгновенным сохранением изменений в локальной базе данных (`Entity Framework Core`) и обновлением файлов `GeoJSON`.
* **Генерация документов (MS Word):** Автоматическое формирование официального «Расписания выездов» на основе актуального списка частей из БД. При добавлении или удалении пожарной части, изменения немедленно учитываются в структуре генерируемого Word-документа.
* **Инструменты измерений:** Интерактивная «резиновая нить» для измерения расстояний и площадей на карте с компенсацией искажений проекции Меркатора.

---

## 📐 Архитектура и Качество кода (SOLID)

Проект спроектирован с упором на слабую связанность компонентов и разделение обязанностей (Separation of Concerns):

* **Single Responsibility (SRP):** Бизнес-логика полностью отделена от отображения. Расчетами, сетью и кэшем занимаются независимые сервисы (`RouteService`, `SearchService`, `MeasureService`), а отрисовкой графики на картах — специализированные визуализаторы (`RouteVisualizer`, `MapVisualizer`).
* **Open/Closed & Liskov (OCP / LSP):** Системы кэширования спроектированы на интерфейсах (`IRouteCache`, `ISearchCache`). Это позволяет легко заменить файловый кэш на Redis или SQLite без изменения логики сервисов.
* **Инверсия зависимостей:** UI-компоненты не управляют логикой ГИС-сервисов напрямую, а получают настроенные сервисы через слои абстракции.

---

## 🛠️ Стек технологий

* **Язык:** C# (.NET 8+ / WPF)
* **ГИС-движок:** Mapsui (WPF)
* **Работа с геометриями:** NetTopologySuite (NTS)
* **База данных / ORM:** Entity Framework Core (EF Core), СУБД SQLite
* **Интеграция с API:** HttpClient, OSRM API (Маршруты и Матрицы), Nominatim API (Геокодинг)
* **Генерация документов:** DocX / Microsoft.Office.Interop.Word / OpenXML
* **Сериализация:** System.Text.Json

---

## 📺 Демонстрация работы

### 1. Расчет маршрутов по рангам пожара
![Расчет маршрутов](https://private-user-images.githubusercontent.com/157400720/616430572-b98ec288-3c72-4ee7-86bd-01a63b65f577.gif?jwt=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3ODMwMTEyNjQsIm5iZiI6MTc4MzAxMDk2NCwicGF0aCI6Ii8xNTc0MDA3MjAvNjE2NDMwNTcyLWI5OGVjMjg4LTNjNzItNGVlNy04NmJkLTAxYTYzYjY1ZjU3Ny5naWY_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjYwNzAyJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI2MDcwMlQxNjQ5MjRaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT03OTAzMDRjOWI5MTdjNmQxZDA0YTc2ZTc1Mzc2ODFiODUxNThmNzRiODVlYTY2Y2MzZjBiNTliMzc2Njg4NDAyJlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCZyZXNwb25zZS1jb250ZW50LXR5cGU9aW1hZ2UlMkZnaWYifQ.ycGJLNvv3oNi4W0uxAXWpflOYXBsnJ1PJK3sfK7ppOA)
*Описание: Система рассчитывает маршруты для нескольких пожарных частей одновременно, пока не закроет потребность ранга по технике.*

### 2. Генерация «Расписания выездов» в Word
![Генерация Word](https://private-user-images.githubusercontent.com/157400720/616395637-0e49fe8f-9dfa-48af-b389-765aedf86718.gif?jwt=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJnaXRodWIuY29tIiwiYXVkIjoicmF3LmdpdGh1YnVzZXJjb250ZW50LmNvbSIsImtleSI6ImtleTUiLCJleHAiOjE3ODMwMDc5NjgsIm5iZiI6MTc4MzAwNzY2OCwicGF0aCI6Ii8xNTc0MDA3MjAvNjE2Mzk1NjM3LTBlNDlmZThmLTlkZmEtNDhhZi1iMzg5LTc2NWFlZGY4NjcxOC5naWY_WC1BbXotQWxnb3JpdGhtPUFXUzQtSE1BQy1TSEEyNTYmWC1BbXotQ3JlZGVudGlhbD1BS0lBVkNPRFlMU0E1M1BRSzRaQSUyRjIwMjYwNzAyJTJGdXMtZWFzdC0xJTJGczMlMkZhd3M0X3JlcXVlc3QmWC1BbXotRGF0ZT0yMDI2MDcwMlQxNTU0MjhaJlgtQW16LUV4cGlyZXM9MzAwJlgtQW16LVNpZ25hdHVyZT03ODYzODI1OTMwZDdlMTQ0MjJlZTc3NDQ5MjMzYjhjN2UwYmM1MWVlZDQwNWFlMTBjZjIxNDcwZDM3M2MxNWQ2JlgtQW16LVNpZ25lZEhlYWRlcnM9aG9zdCZyZXNwb25zZS1jb250ZW50LXR5cGU9aW1hZ2UlMkZnaWYifQ.j_oNMynlH7HB0tTGf0OiqcZwY9yPg8i3oph84P8AumQ)
*Описание: Документ формируется одной кнопкой, автоматически подтягивая изменения в составе пожарных частей из базы данных.*

---

## ⚙️ Как запустить проект

1. Склонируйте репозиторий:
   ```bash
   git clone https://github.com/KuvoKuvo/FFSchedule.git
