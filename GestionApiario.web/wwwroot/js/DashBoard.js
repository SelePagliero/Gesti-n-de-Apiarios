window.renderGraficoEnfermedades = function (dataEnfermedades) {
    var ctx = document.getElementById("enfermedadesGrafico");
    if (!ctx) return;

    // Si ya había un gráfico (por ejemplo, al cambiar de apicultor en el tablero), se reemplaza.
    if (window.graficoEnfermedades) {
        window.graficoEnfermedades.destroy();
    }

    window.graficoEnfermedades = new Chart(ctx, {
        type: 'doughnut',
        data: dataEnfermedades,
        options: {
            maintainAspectRatio: false,
            tooltips: {
                backgroundColor: "rgb(255,255,255)",
                bodyFontColor: "#858796",
                borderColor: '#dddfeb',
                borderWidth: 1,
                xPadding: 15,
                yPadding: 15,
                displayColors: false,
                caretPadding: 10,
            },
            legend: {
                display: true
            },
            cutoutPercentage: 80,
        },
    });
};
