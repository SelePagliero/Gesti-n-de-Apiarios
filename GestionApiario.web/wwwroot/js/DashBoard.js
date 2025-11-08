window.renderGraficoEnfermedades = function (dataEnfermedades) {
    var ctx = document.getElementById("enfermedadesGrafico");
    var myPieChart = new Chart(ctx, {
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
