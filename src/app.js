// ============================================
// ДАННЫЕ НОРМАТИВОВ (внешний файл data.js)
// ============================================

// ============================================
// МАППИНГ ДАННЫХ
// ============================================

const dataMapping = {
    'woman-stadium': womanStadiumData,
    'man-stadium': manStadiumData
};

// ============================================
// ЛОГИКА ПРИЛОЖЕНИЯ
// ============================================

document.addEventListener('DOMContentLoaded', function() {
    const genderSelect = document.getElementById('gender');
    const locationSelect = document.getElementById('location');
    const distanceSelect = document.getElementById('distance');
    const measurementSelect = document.getElementById('measurement');
    const circleSelect = document.getElementById('circle');
    const resultsDiv = document.getElementById('results');
    const placeholder = document.getElementById('placeholder');

    let currentData = null;

    // Скрываем placeholder изначально
    placeholder.style.display = 'block';

    genderSelect.addEventListener('change', function() {
        locationSelect.value = '';
        locationSelect.disabled = !this.value;
        distanceSelect.value = '';
        distanceSelect.disabled = true;
        measurementSelect.value = '';
        measurementSelect.disabled = true;
        circleSelect.value = '';
        circleSelect.disabled = true;
        currentData = null;
        showPlaceholder();
    });

    locationSelect.addEventListener('change', function() {
        distanceSelect.value = '';
        distanceSelect.disabled = !this.value;
        measurementSelect.value = '';
        measurementSelect.disabled = true;
        circleSelect.value = '';
        circleSelect.disabled = true;
        currentData = null;
        showPlaceholder();

        if (this.value && genderSelect.value) {
            loadDistanceOptions();
        }
    });

    distanceSelect.addEventListener('change', function() {
        measurementSelect.value = '';
        measurementSelect.disabled = !this.value;
        measurementSelect.innerHTML = '<option value="">Все</option><option value="ручной">Ручной</option><option value="авто">Авто</option>';
        circleSelect.value = '';
        circleSelect.disabled = true;

        if (this.value) {
            loadMeasurementOptions();
            loadCircleOptions();
            displayResults();
        } else {
            showPlaceholder();
        }
    });

    measurementSelect.addEventListener('change', function() {
        circleSelect.value = '';
        circleSelect.disabled = !this.value;

        if (this.value) {
            loadCircleOptions();
        } else {
            loadCircleOptions();
        }
        displayResults();
    });

    circleSelect.addEventListener('change', function() {
        displayResults();
    });

    function showPlaceholder() {
        placeholder.style.display = 'block';
        const cards = resultsDiv.querySelectorAll('.card, .alert-info');
        cards.forEach(card => card.style.display = 'none');
    }

    function hidePlaceholder() {
        placeholder.style.display = 'none';
    }

    function loadDistanceOptions() {
        const key = `${genderSelect.value}-${locationSelect.value}`;
        currentData = dataMapping[key];

        if (!currentData) return;

        const distances = [...new Set(currentData.map(item => item['Дистанция']))];

        distanceSelect.innerHTML = '<option value="">Выберите...</option>';
        distances.forEach(distance => {
            const option = document.createElement('option');
            option.value = distance;
            option.textContent = distance;
            distanceSelect.appendChild(option);
        });

        distanceSelect.disabled = false;
    }

    function loadMeasurementOptions() {
        const selectedDistance = distanceSelect.value;
        const filteredData = currentData.filter(item => item['Дистанция'] === selectedDistance);
        const measurements = [...new Set(filteredData.map(item => item['Хронометраж']))];

        measurementSelect.innerHTML = '<option value="">Все</option>';
        measurements.forEach(measurement => {
            const option = document.createElement('option');
            option.value = measurement;
            option.textContent = measurement === 'ручной' ? 'Ручной' : 'Авто';
            measurementSelect.appendChild(option);
        });

        measurementSelect.disabled = false;
    }

    function loadCircleOptions() {
        const selectedDistance = distanceSelect.value;
        const selectedMeasurement = measurementSelect.value;

        let filteredData = currentData.filter(item => item['Дистанция'] === selectedDistance);

        if (selectedMeasurement) {
            filteredData = filteredData.filter(item => item['Хронометраж'] === selectedMeasurement);
        }

        const circles = [...new Set(filteredData.map(item => item['Круг']))];
        const hasNonNullCircles = circles.some(circle => circle !== null);

        circleSelect.innerHTML = '<option value="">Все</option>';
        if (hasNonNullCircles) {
            circles.forEach(circle => {
                if (circle !== null) {
                    const option = document.createElement('option');
                    option.value = circle;
                    option.textContent = circle;
                    circleSelect.appendChild(option);
                }
            });
            circleSelect.disabled = false;
        } else {
            circleSelect.disabled = true;
        }
    }

    function displayResults() {
        const selectedDistance = distanceSelect.value;
        const selectedMeasurement = measurementSelect.value;
        const selectedCircle = circleSelect.value;

        if (!currentData || !selectedDistance) {
            showPlaceholder();
            return;
        }

        let filteredData = currentData.filter(item => item['Дистанция'] === selectedDistance);

        if (selectedMeasurement) {
            filteredData = filteredData.filter(item => item['Хронометраж'] === selectedMeasurement);
        }

        if (selectedCircle) {
            filteredData = filteredData.filter(item => item['Круг'] === selectedCircle);
        }

        if (filteredData.length === 0) {
            resultsDiv.innerHTML = `
                <div class="alert alert-info text-center">
                    Нет данных для выбранных параметров
                </div>
            `;
            return;
        }

        hidePlaceholder();

        // Удаляем старые результаты
        const oldResults = resultsDiv.querySelectorAll('.card, .alert-info');
        oldResults.forEach(el => el.remove());

        const resultsContainer = document.createElement('div');
        resultsContainer.className = 'table-responsive mx-auto';
        resultsContainer.style.maxWidth = '800px';
        resultsContainer.style.width = '100%';

        filteredData.forEach(item => {
            const measurementType = item['Хронометраж'] === 'авто' ? 'Автоматический' : 'Ручной';
            const circleInfo = item['Круг'] ? ` (круг ${item['Круг']})` : '';
            
            // Определяем акцентный цвет для заголовка на основе типа хронометража
            const accentColor = item['Хронометраж'] === 'авто' ? 'var(--accent-measurement)' : 'var(--accent-location)';

            const card = document.createElement('div');
            card.className = 'card mb-3';
            card.innerHTML = `
                <div class="card-header">
                    <strong>${item['Дистанция']}${circleInfo}</strong> — ${measurementType}
                </div>
                <div class="card-body p-0">
                    <table class="table mb-0">
                        <thead>
                            <tr>
                                <th>Разряд</th>
                                <th>Время</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${item['Разряды'].map(rank => `
                                <tr>
                                    <td>${rank['Разряд']}</td>
                                    <td>${rank['Время']}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            `;
            resultsContainer.appendChild(card);
        });

        resultsDiv.appendChild(resultsContainer);
    }
});
