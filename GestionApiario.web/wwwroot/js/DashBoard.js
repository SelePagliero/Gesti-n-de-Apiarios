window.renderGraficoEnfermedades = function (dataEnfermedades) {
    var ctx = document.getElementById("enfermedadesGrafico");
    if (!ctx) return;

    // Si ya había un gráfico (por ejemplo, al cambiar de apicultor en el tablero), se reemplaza.
    if (window.graficoEnfermedades) {
        window.graficoEnfermedades.destroy();
    }

    // Misma tipografía y colores de texto que el resto del sistema (tema "miel cálido").
    Chart.defaults.global.defaultFontFamily = "'Nunito', 'Segoe UI', system-ui, sans-serif";
    Chart.defaults.global.defaultFontColor = "#7D6A5C";

    window.graficoEnfermedades = new Chart(ctx, {
        type: 'doughnut',
        data: dataEnfermedades,
        options: {
            maintainAspectRatio: false,
            tooltips: {
                backgroundColor: "#3E2416",
                titleFontColor: "#FBF6EC",
                bodyFontColor: "#FBF6EC",
                xPadding: 12,
                yPadding: 10,
                cornerRadius: 8,
                displayColors: true,
                caretPadding: 8,
                callbacks: {
                    label: function (item, datos) {
                        return " " + datos.labels[item.index] + ": " + datos.datasets[0].data[item.index] + "%";
                    }
                }
            },
            legend: {
                display: true,
                // En pantallas chicas la leyenda va abajo para que el gráfico no quede angosto.
                position: window.innerWidth < 768 ? 'bottom' : 'right',
                labels: {
                    boxWidth: 14,
                    padding: 16,
                    fontSize: 13
                }
            },
            cutoutPercentage: 65,
            elements: {
                arc: {
                    borderWidth: 3,
                    borderColor: "#FFFFFF"
                }
            }
        },
    });
};
