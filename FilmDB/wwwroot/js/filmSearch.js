document.addEventListener("DOMContentLoaded", function ()
{
    let selectedGenres = []; // Store selected genre IDs
    let minimumYear = 1910;
    let maximumYear = 2025;
    let loadedMinYear = 1910; // Track what data we actually have loaded
    let loadedMaxYear = 2025;

    // Check for pre-selected genres from URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    const genreIdsParam = urlParams.get('genreIds');
    const startYearParam = urlParams.get('startYear');
    const endYearParam = urlParams.get('endYear');

    // Pre-select genre buttons if coming from graph
    if (genreIdsParam)
    {
        selectedGenres = genreIdsParam.split(',').map(id => Number.parseInt(id));
        selectedGenres.forEach(genreId =>
        {
            const button = document.querySelector(`.genre-button[data-id="${genreId}"]`);
            if (button)
            {
                button.classList.remove('btn-secondary');
                button.classList.add('btn-primary');
            }
        });
    }

    // Pre-set year range if coming from graph
    if (startYearParam && endYearParam)
    {
        minimumYear = Number.parseInt(startYearParam);
        maximumYear = Number.parseInt(endYearParam);
        loadedMinYear = minimumYear; // Track loaded data range
        loadedMaxYear = maximumYear;
    }

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
            const genreId = Number.parseInt(this.dataset.id);

            // Toggle genre selection
            if (selectedGenres.includes(genreId))
            {
                selectedGenres = selectedGenres.filter(id => id !== genreId); // Remove if already selected
                this.classList.remove("btn-primary");
                this.classList.add("btn-secondary");
                updateFilmTable();
            }
            else if(selectedGenres.length < 3)
            {
                selectedGenres.push(genreId); // Add if not selected
                this.classList.remove("btn-secondary");
                this.classList.add("btn-primary");
                updateFilmTable();
            }
			else
            {
                this.classList.add("btn-danger"); // Temporarily add red color
                setTimeout(() => this.classList.remove("btn-danger"), 250);
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
                // Reset loaded year range to full range after genre change
                loadedMinYear = 1910;
                loadedMaxYear = 2025;
                filterFilmsByYear();
            })
            .catch(error => console.error("Error loading films:", error));
    }

    // Year slider logic
    let slider = document.getElementById("yearSlider");
    noUiSlider.create(slider, {
        start: [minimumYear, maximumYear], // Use pre-set values
        connect: true,
        range: { 'min': 1910, 'max': 2025 },
        step: 1
    });

    // Initial setup: Create the labels and place them on the handles
    let minLabel = document.createElement("div");
    minLabel.id = "minYearLabel";
    minLabel.classList.add("slider-label");
    slider.querySelector('.noUi-handle-lower').appendChild(minLabel);

    let maxLabel = document.createElement("div");
    maxLabel.id = "maxYearLabel";
    maxLabel.classList.add("slider-label");
    slider.querySelector('.noUi-handle-upper').appendChild(maxLabel);

    // Initial call to set labels
    updateHandleLabels();

    // Apply year filter on initial load if pre-selected
    if (startYearParam && endYearParam)
    {
        filterFilmsByYear();
    }

    slider.noUiSlider.on("set", function (values)
    {
        const newMinYear = Math.round(values[0]);
        const newMaxYear = Math.round(values[1]);

        // Check if user is trying to expand beyond loaded data
        if (newMinYear < loadedMinYear || newMaxYear > loadedMaxYear)
        {
            // Need to reload data with expanded range
            minimumYear = newMinYear;
            maximumYear = newMaxYear;
            loadedMinYear = newMinYear;
            loadedMaxYear = newMaxYear;
            updateHandleLabels();
            reloadFilmsWithYearRange(newMinYear, newMaxYear);
        }
        else
        {
            // Can filter client-side
            minimumYear = newMinYear;
            maximumYear = newMaxYear;
            filterFilmsByYear();
            updateHandleLabels();
        }
    });

    // update labels when the slider is updated
    slider.noUiSlider.on("update", function (values)
    {
        minimumYear = Math.round(values[0]);
        maximumYear = Math.round(values[1]);
        updateHandleLabels();
    });

    // update the text labels on the handles to show selected years
    function updateHandleLabels()
    {
        let minYearLabel = document.getElementById("minYearLabel");
        let maxYearLabel = document.getElementById("maxYearLabel");
        if (minYearLabel && maxYearLabel)
        {
            minYearLabel.textContent = minimumYear;
            maxYearLabel.textContent = maximumYear;
        }
    }

    function filterFilmsByYear()
    {
        // Filter films by selected year range
        let rows = document.querySelectorAll("tr[data-year]");
        let visibleFilmCount = 0;
        rows.forEach(function (row)
        {
            let filmYear = Number.parseInt(row.getAttribute("data-year"));
            if (filmYear >= minimumYear && filmYear <= maximumYear)
            {
                row.style.display = "";
                visibleFilmCount++;
            } else
            {
                row.style.display = "none";
            }
        });
        // Update the film count in the span
        let filmCountElement = document.getElementById("film-count");
        if (filmCountElement != null)
        {
            if (minimumYear != maximumYear)
            {
                filmCountElement.textContent = `${minimumYear} to ${maximumYear}: showing ${visibleFilmCount} films`;
            }
            else
            {
                filmCountElement.textContent = `${minimumYear}: showing ${visibleFilmCount} films`;
            }
        }
    }

    // Reload films with new year range from server
    function reloadFilmsWithYearRange(minYear, maxYear)
    {
        const container = document.getElementById("filmTableContainer");
        container.innerHTML = "<p>Loading...</p>";

        // Build query string with genres and year range
        let queryString = selectedGenres.length > 0
            ? `?genreIds=${selectedGenres.join("&genreIds=")}&startYear=${minYear}&endYear=${maxYear}`
            : `?startYear=${minYear}&endYear=${maxYear}`;

        fetch(`/Film/FilterFilmsByGenreBitwiseWithYearRange${queryString}`)
            .then(response => response.text())
            .then(data =>
            {
                container.innerHTML = data;
                filterFilmsByYear(); // Apply any additional client-side filtering
            })
            .catch(error => console.error("Error loading films:", error));
    }
});