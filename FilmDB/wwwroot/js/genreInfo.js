var ctx = document.getElementById('genreChart').getContext('2d');
var genreChart = new Chart(ctx, {
	type: 'bar', // or 'line' for line chart
	data: {
		labels: @Html.Raw(Json.Serialize(Model.FilmYears.Select(f => f.Year).ToList())),
		datasets: [{
			label: 'Film Count',
			data: @Html.Raw(Json.Serialize(Model.FilmYears.Select(f => f.FilmCount).ToList())),
			backgroundColor: 'rgba(54, 162, 235, 0.8)',
			borderColor: 'rgba(54, 162, 235, 1)',
			borderWidth: 1
		}]
	},
	options:
	{
		responsive: true,
		onClick: function (event, elements)
		{
			if (elements.length > 0)
			{
				const index = elements[0].index; // Get the index of the clicked bar
				const year = this.data.labels[index]; // Get the year from the label
				const genre = "@Model.Name";

				// Navigate to the Genre-Year detail page
				const url = `/Film/GenreYearDetail?genre=${genre}&year=${year}`;
				window.location.href = url; // Redirect to the new page
			}
		},
		scales:
		{
			y: {
				beginAtZero: true
			}
		},
		plugins:
		{
			legend:
			{
				display: false // This will hide the legend
			}
		}
	}
});