document.addEventListener("DOMContentLoaded", function ()
{
	let selectedGenres = []; // Store selected genre IDs
	let minimumYear = 1910;
	let maximumYear = 2025;

	document.getElementById("filmSearchForm").addEventListener("submit", function (event)
	{
		event.preventDefault(); // Prevent full page reload
		const query = document.getElementById("filmSearchQuery").value;
		fetch(`/Film/FilmSearch?query=${encodeURIComponent(query)}`)
			.then(response => response.text())
			.then(data =>
			{
				document.getElementById("filmTableContainer").innerHTML = data;
			})
			.catch(error => console.error("Error loading films:", error));
	});

	//controls for genre buttons to requery
	document.querySelectorAll(".genre-button").forEach(button =>
	{
		button.addEventListener("click", function ()
		{
			const genreId = parseInt(this.dataset.id);

			// Toggle genre selection
			if (selectedGenres.includes(genreId))
			{
				selectedGenres = selectedGenres.filter(id => id !== genreId); // Remove if already selected
				this.classList.remove("btn-primary");
				this.classList.add("btn-secondary"); // Highlight selected
				updateFilmTable();
			}
			else
			{
				if (selectedGenres.length < 3)
				{
					selectedGenres.push(genreId); // Add if not selected
					this.classList.remove("btn-secondary"); // Reset color
					this.classList.add("btn-primary");
					updateFilmTable();
				}
				else
				{
					this.classList.add("btn-danger"); // Temporarily add red color
					setTimeout(() => this.classList.remove("btn-danger"), 250); // Remove after 0.5 sec
				}
			}
		});
	});

	function updateFilmTable()
	{
		const queryString = selectedGenres.length > 0 ? `?genreIds=${selectedGenres.join("&genreIds=")}` : "";
		const container = document.getElementById("filmTableContainer")
		container.innerHTML = "<p>Loading...</p>";
		fetch(`/Film/FilterFilmsByGenreBitwise${queryString}`)
			.then(response => response.text())
			.then(data =>
			{
				container.innerHTML = data;
				filterFilmsByYear();
			})
			.catch(error => console.error("Error loading films:", error));
	}

	// Year slider logic
	var slider = document.getElementById("yearSlider");
	noUiSlider.create(slider, {
		start: [minimumYear, maximumYear],
		connect: true,
		range: { 'min': minimumYear, 'max': maximumYear },
		step: 1
	});

	// Initial setup: Create the labels and place them on the handles
	var minLabel = document.createElement("div");
	minLabel.id = "minYearLabel";
	minLabel.classList.add("slider-label"); // Add a class for styling
	slider.querySelector('.noUi-handle-lower').appendChild(minLabel);

	var maxLabel = document.createElement("div");
	maxLabel.id = "maxYearLabel";
	maxLabel.classList.add("slider-label"); // Add a class for styling
	slider.querySelector('.noUi-handle-upper').appendChild(maxLabel);

	// Initial call to set labels
	updateHandleLabels(slider.noUiSlider.values);

	slider.noUiSlider.on("set", function (values)
	{
		minimumYear = Math.round(values[0]);
		maximumYear = Math.round(values[1]);
		filterFilmsByYear(values);
		updateHandleLabels(values);
	});

	// update labels when the slider is updated
	slider.noUiSlider.on("update", function (values)
	{
		minimumYear = Math.round(values[0]);
		maximumYear = Math.round(values[1]);
		updateHandleLabels(values);
	});

	// update the text labels on the handles to show selected years
	function updateHandleLabels(values)
	{
		var minYearLabel = document.getElementById("minYearLabel");
		var maxYearLabel = document.getElementById("maxYearLabel");
		var slider = document.getElementById("yearSlider");
		if (minYearLabel && maxYearLabel)
		{
			minYearLabel.textContent = minimumYear;  // Update text for the first handle
			maxYearLabel.textContent = maximumYear;  // Update text for the second handle
		}
	}
	function filterFilmsByYear(values)
	{
		// Filter films by selected year range
		var rows = document.querySelectorAll("tr[data-year]");  // Get all rows with a data-year attribute
		var visibleFilmCount = 0;
		rows.forEach(function (row)
		{
			var filmYear = parseInt(row.getAttribute("data-year"));
			if (filmYear >= minimumYear && filmYear <= maximumYear)
			{
				row.style.display = "";  // Show the row
				visibleFilmCount++;
			} else
			{
				row.style.display = "none";  // Hide the row
			}
		});
		// Update the film count in the span
		var filmCountElement = document.getElementById("film-count");
		if (minimumYear != maximumYear)
		{
			filmCountElement.textContent = `${minimumYear} to ${maximumYear}: showing ${visibleFilmCount} films`;
		}
		else
		{
			filmCountElement.textContent = `${minimumYear}: showing ${visibleFilmCount} films`;
		}
	}
});