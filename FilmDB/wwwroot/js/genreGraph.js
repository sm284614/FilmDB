document.addEventListener("DOMContentLoaded", function ()
{
	// Check if we're on the Genre page by looking for required elements
	const genreButtons = document.querySelectorAll(".genre-button");
	const slider = document.getElementById("yearSlider");

	// If neither genre buttons nor slider exist, we're not on the Genre page - exit early
	if (genreButtons.length === 0 && !slider)
	{
		console.log("Genre page elements not found - skipping genreGraph.js initialization");
		return;
	}

	let minimumYear = 1910;
	let maximumYear = 2025;
	let graphMode = 1; // 1 = Compare, 2 = Combine

	let genreChart; // Store chart instance globally
	let fullLabels = [];
	let fullData = [];
	let selectedGenres = []; // Store selected genre IDs
	let updateTimeout = null; // For debouncing year slider updates

	// Genre button click handler
	genreButtons.forEach(button =>
	{
		button.addEventListener("click", function ()
		{
			var genreId = this.dataset.id;
			var adding;

			if (selectedGenres.includes(genreId))
			{
				// Remove genre from list and reset button class
				selectedGenres = selectedGenres.filter(id => id !== genreId);
				this.classList.remove("btn-primary");
				this.classList.add("btn-secondary");
				adding = false;
			}
			else
			{
				// Add genre to list and set button as selected
				selectedGenres.push(genreId);
				this.classList.remove("btn-secondary");
				this.classList.add("btn-primary");
				adding = true;
			}

			// Update the graph based on current mode
			updateGraph();
		});
	});

	// Mode selection button handler
	document.querySelectorAll(".btn-mode-select").forEach(button =>
	{
		button.addEventListener("click", function ()
		{
			let selectedMode = parseInt(this.dataset.mode);

			if (graphMode !== selectedMode)
			{
				graphMode = selectedMode;

				// Reset all buttons
				document.querySelectorAll(".btn-mode-select").forEach(btn =>
				{
					btn.classList.remove("btn-primary");
					btn.classList.add("btn-secondary");
				});

				// Highlight the selected button
				this.classList.remove("btn-secondary");
				this.classList.add("btn-primary");

				// Refresh graph with new mode if genres are selected
				if (selectedGenres.length > 0)
				{
					updateGraph();
				}
			}
		});
	});

	// Main function to update graph based on mode and selected genres
	function updateGraph()
	{
		const container = document.getElementById("genreGraphContainer");

		if (selectedGenres.length === 0)
		{
			// Clear graph if no genres selected
			container.innerHTML = "<p class='text-muted'>Select one or more genres to view data</p>";
			if (genreChart)
			{
				genreChart.destroy();
				genreChart = null;
			}
			return;
		}

		if (graphMode === 1) // Compare mode
		{
			updateCompareMode(container);
		}
		else if (graphMode === 2) // Combine mode
		{
			updateCombineMode(container);
		}
	}

	// Compare mode: Show separate line for each genre
	function updateCompareMode(container)
	{
		if (selectedGenres.length === 1)
		{
			// Single genre - load initial chart HTML
			container.innerHTML = "<p>Loading...</p>";
			const genreId = selectedGenres[0];

			fetch(`/Genre/GenreGraph?genre_id=${genreId}`)
				.then(response => response.text())
				.then(data =>
				{
					container.innerHTML = data;
					initializeChart();
				})
				.catch(error =>
				{
					container.innerHTML = "<p class='text-danger'>Error loading data.</p>";
					console.error(error);
				});
		}
		else
		{
			// Multiple genres - load all genre data
			loadCompareData(container);
		}
	}

	// Load data for compare mode with multiple genres
	function loadCompareData(container)
	{
		container.innerHTML = "<p>Loading...</p>";

		// Fetch data for all selected genres
		const promises = selectedGenres.map(genreId =>
			fetch(`/Genre/GenreGraphData?genre_id=${genreId}`)
				.then(response => response.json())
		);

		Promise.all(promises)
			.then(results =>
			{
				// Create chart canvas if it doesn't exist
				if (!document.getElementById('genreChart'))
				{
					container.innerHTML = '<canvas id="genreChart" width="400" height="200"></canvas>';
				}

				createMultiGenreChart(results, false);
			})
			.catch(error =>
			{
				container.innerHTML = "<p class='text-danger'>Error loading data.</p>";
				console.error(error);
			});
	}

	// Combine mode: Show single line for films with ALL selected genres
	function updateCombineMode(container)
	{
		container.innerHTML = "<p>Loading...</p>";

		// Join genre IDs with comma for backend
		const genreIds = selectedGenres.join(',');

		fetch(`/Genre/CombinedGenreGraphData?genre_ids=${genreIds}`)
			.then(response => response.json())
			.then(data =>
			{
				// Create chart canvas if it doesn't exist
				if (!document.getElementById('genreChart'))
				{
					container.innerHTML = '<canvas id="genreChart" width="400" height="200"></canvas>';
				}

				createCombinedChart(data);
			})
			.catch(error =>
			{
				container.innerHTML = "<p class='text-danger'>Error loading combined data.</p>";
				console.error(error);
			});
	}

	// Create chart for multiple genres (compare mode)
	function createMultiGenreChart(genreDataArray, isCombined)
	{
		const chartElement = document.getElementById('genreChart');
		if (!chartElement) return;

		const ctx = chartElement.getContext('2d');

		// Collect all unique years across all genres
		let allYears = new Set();
		genreDataArray.forEach(genreData =>
		{
			genreData.years.forEach(year => allYears.add(year));
		});

		// Sort years
		let sortedYears = Array.from(allYears).sort((a, b) => a - b);

		// Filter by year range
		let filteredYears = sortedYears.filter(year => year >= minimumYear && year <= maximumYear);

		// Create datasets for each genre
		let datasets = genreDataArray.map((genreData, index) =>
		{
			// Create data array aligned with filteredYears
			let alignedData = filteredYears.map(year =>
			{
				let yearIndex = genreData.years.indexOf(year);
				return yearIndex !== -1 ? genreData.counts[yearIndex] : 0;
			});

			return {
				label: genreData.genreName,
				data: alignedData,
				borderColor: getColorForIndex(index),
				backgroundColor: getColorForIndex(index, 0.1),
				borderWidth: 2,
				fill: false,
				tension: 0.1
			};
		});

		// Store full data for year filtering
		fullLabels = sortedYears;
		fullData = genreDataArray;

		// Destroy existing chart
		if (genreChart)
		{
			genreChart.destroy();
		}

		// Create new chart
		genreChart = new Chart(ctx, {
			type: 'line',
			data: {
				labels: filteredYears,
				datasets: datasets
			},
			options: {
				responsive: true,
				interaction: {
					mode: 'index',
					intersect: false,
				},
				onClick: function (event, elements)
				{
					if (elements.length > 0)
					{
						const datasetIndex = elements[0].datasetIndex;
						const index = elements[0].index;
						const year = this.data.labels[index];
						const genreName = this.data.datasets[datasetIndex].label;

						// Navigate to the genre-year detail page
						window.location.href = `/Film/GenreYearDetail?genre=${encodeURIComponent(genreName)}&year=${year}`;
					}
				},
				scales: {
					y: {
						beginAtZero: true,
						ticks: {
							stepSize: 1
						}
					}
				},
				plugins: {
					legend: {
						display: true,
						position: 'top',
						onClick: function (e, legendItem, legend)
						{
							// Default legend click behavior (toggle visibility)
							const index = legendItem.datasetIndex;
							const chart = legend.chart;
							const meta = chart.getDatasetMeta(index);
							meta.hidden = meta.hidden === null ? !chart.data.datasets[index].hidden : null;
							chart.update();
						}
					},
					tooltip: {
						callbacks: {
							title: function (context)
							{
								return 'Year: ' + context[0].label;
							},
							label: function (context)
							{
								return context.dataset.label + ': ' + context.parsed.y + ' films';
							}
						}
					}
				}
			}
		});
	}

	// Create chart for combined genres
	function createCombinedChart(data)
	{
		const chartElement = document.getElementById('genreChart');
		if (!chartElement) return;

		const ctx = chartElement.getContext('2d');

		// Filter data by year range
		let filteredLabels = [];
		let filteredData = [];

		for (let i = 0; i < data.years.length; i++)
		{
			let year = data.years[i];
			if (year >= minimumYear && year <= maximumYear)
			{
				filteredLabels.push(year);
				filteredData.push(data.counts[i]);
			}
		}

		// Store full data for year filtering
		fullLabels = data.years;
		fullData = data.counts;

		// Destroy existing chart
		if (genreChart)
		{
			genreChart.destroy();
		}

		// Create new chart
		genreChart = new Chart(ctx, {
			type: 'line',
			data: {
				labels: filteredLabels,
				datasets: [{
					label: data.genreName || 'Combined Genres',
					data: filteredData,
					backgroundColor: 'rgba(54, 162, 235, 0.2)',
					borderColor: 'rgba(54, 162, 235, 1)',
					borderWidth: 2,
					fill: true,
					tension: 0.1
				}]
			},
			options: {
				responsive: true,
				onClick: function (event, elements)
				{
					if (elements.length > 0)
					{
						const index = elements[0].index;
						const year = this.data.labels[index];
						const genreName = this.data.datasets[0].label;
						// For combined genres, navigate with the combined genre name
						// The backend will need to handle this appropriately
						window.location.href = `/Film/GenreYearDetail?genre=${encodeURIComponent(genreName)}&year=${year}`;
					}
				},
				scales: {
					y: {
						beginAtZero: true,
						ticks: {
							stepSize: 1
						}
					}
				},
				plugins: {
					legend: {
						display: true,
						position: 'top'
					},
					tooltip: {
						callbacks: {
							label: function (context)
							{
								return context.dataset.label + ': ' + context.parsed.y + ' films';
							}
						}
					}
				}
			}
		});
	}

	// Initialize chart from server-rendered HTML (for single genre)
	function initializeChart()
	{
		const chartElement = document.getElementById('genreChart');
		if (!chartElement) return;

		const ctx = chartElement.getContext('2d');
		const genre = chartElement.getAttribute("data-genre");
		fullLabels = JSON.parse(chartElement.getAttribute("data-labels"));
		fullData = JSON.parse(chartElement.getAttribute("data-data"));

		let filteredLabels = [];
		let filteredData = [];

		for (let i = 0; i < fullLabels.length; i++)
		{
			let year = parseInt(fullLabels[i]);
			if (year >= minimumYear && year <= maximumYear)
			{
				filteredLabels.push(fullLabels[i]);
				filteredData.push(fullData[i]);
			}
		}

		if (genreChart)
		{
			genreChart.destroy();
		}

		genreChart = new Chart(ctx, {
			type: 'line',
			data: {
				labels: filteredLabels,
				datasets: [{
					label: genre,
					data: filteredData,
					backgroundColor: 'rgba(54, 162, 235, 0.2)',
					borderColor: 'rgba(54, 162, 235, 1)',
					borderWidth: 2,
					fill: true,
					tension: 0.1
				}]
			},
			options: {
				responsive: true,
				onClick: function (event, elements)
				{
					if (elements.length > 0)
					{
						const index = elements[0].index;
						const year = this.data.labels[index];
						window.location.href = `/Film/GenreYearDetail?genre=${genre}&year=${year}`;
					}
				},
				scales: {
					y: {
						beginAtZero: true,
						ticks: {
							stepSize: 1
						}
					}
				},
				plugins: {
					legend: {
						display: true,
						position: 'top'
					},
					tooltip: {
						callbacks: {
							label: function (context)
							{
								return context.dataset.label + ': ' + context.parsed.y + ' films';
							}
						}
					}
				}
			}
		});
	}

	// Update chart when year range changes
	function updateChartYearRange(minYear, maxYear)
	{
		if (!genreChart) return;

		if (graphMode === 2 && typeof fullData === 'object' && !Array.isArray(fullData))
		{
			// Combine mode with simple data structure
			let filteredLabels = [];
			let filteredData = [];

			for (let i = 0; i < fullLabels.length; i++)
			{
				let year = fullLabels[i];
				if (year >= minYear && year <= maxYear)
				{
					filteredLabels.push(fullLabels[i]);
					filteredData.push(fullData[i]);
				}
			}

			genreChart.data.labels = filteredLabels;
			genreChart.data.datasets[0].data = filteredData;
		}
		else if (graphMode === 1 && Array.isArray(fullData))
		{
			// Compare mode with multiple genres
			let filteredYears = fullLabels.filter(year => year >= minYear && year <= maxYear);

			genreChart.data.labels = filteredYears;

			// Update each dataset
			genreChart.data.datasets.forEach((dataset, index) =>
			{
				let genreData = fullData[index];
				let alignedData = filteredYears.map(year =>
				{
					let yearIndex = genreData.years.indexOf(year);
					return yearIndex !== -1 ? genreData.counts[yearIndex] : 0;
				});
				dataset.data = alignedData;
			});
		}
		else
		{
			// Single genre mode
			let filteredLabels = [];
			let filteredData = [];

			for (let i = 0; i < fullLabels.length; i++)
			{
				let year = parseInt(fullLabels[i]);
				if (year >= minYear && year <= maxYear)
				{
					filteredLabels.push(fullLabels[i]);
					filteredData.push(fullData[i]);
				}
			}

			genreChart.data.labels = filteredLabels;
			genreChart.data.datasets[0].data = filteredData;
		}

		genreChart.update();
	}

	// Helper function to get consistent colors for genres
	function getColorForIndex(index)
	{
		const colors = [
			'rgb(54, 162, 235)',   // Blue
			'rgb(255, 99, 132)',   // Red
			'rgb(75, 192, 192)',   // Green
			'rgb(255, 159, 64)',   // Orange
			'rgb(153, 102, 255)',  // Purple
			'rgb(255, 205, 86)',   // Yellow
			'rgb(201, 203, 207)',  // Grey
			'rgb(255, 99, 255)',   // Pink
			'rgb(99, 255, 132)',   // Light Green
			'rgb(132, 99, 255)'    // Light Purple
		];
		return colors[index % colors.length];
	}

	// Year slider setup (if it exists)
	if (slider)
	{
		noUiSlider.create(slider, {
			start: [minimumYear, maximumYear],
			connect: true,
			range: { 'min': minimumYear, 'max': maximumYear },
			step: 1
		});

		// Create labels on handles
		const minLabel = document.createElement("div");
		minLabel.id = "minYearLabel";
		minLabel.classList.add("slider-label");
		slider.querySelector('.noUi-handle-lower').appendChild(minLabel);

		const maxLabel = document.createElement("div");
		maxLabel.id = "maxYearLabel";
		maxLabel.classList.add("slider-label");
		slider.querySelector('.noUi-handle-upper').appendChild(maxLabel);

		// Update labels immediately
		updateHandleLabels();

		// Update labels during sliding (immediate visual feedback)
		slider.noUiSlider.on("update", function (values)
		{
			minimumYear = Math.round(values[0]);
			maximumYear = Math.round(values[1]);
			updateHandleLabels();
		});

		// Update chart when slider is released (debounced for performance)
		slider.noUiSlider.on("set", function (values)
		{
			minimumYear = Math.round(values[0]);
			maximumYear = Math.round(values[1]);

			// Debounce chart updates for better performance
			if (updateTimeout)
			{
				clearTimeout(updateTimeout);
			}

			updateTimeout = setTimeout(() =>
			{
				updateChartYearRange(minimumYear, maximumYear);
			}, 300);
		});
	}

	// Update the text labels on the handles
	function updateHandleLabels()
	{
		const minYearLabel = document.getElementById("minYearLabel");
		const maxYearLabel = document.getElementById("maxYearLabel");

		if (minYearLabel && maxYearLabel)
		{
			minYearLabel.textContent = minimumYear;
			maxYearLabel.textContent = maximumYear;
		}
	}
});