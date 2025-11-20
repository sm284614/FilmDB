// wordCloud.js - Character word cloud initialization and search handling

window.addEventListener('load', function ()
{
    let searchPerformed = false;

    // Character search form handler
    const searchForm = document.getElementById("characterSearchForm");
    if (searchForm)
    {
        searchForm.addEventListener("submit", function (event)
        {
            event.preventDefault();
            searchPerformed = true;

            // Hide word cloud when search is performed
            const wordCloudContainer = document.getElementById("wordCloudContainer");
            if (wordCloudContainer)
            {
                wordCloudContainer.style.display = "none";
            }

            document.getElementById("characterTableContainer").innerHTML = "<p>Loading data...</p>";
            const query = document.getElementById("characterSearchQuery").value;
            fetch(`/Character/CharacterSearch?query=${encodeURIComponent(query)}`)
                .then(response => response.text())
                .then(data =>
                {
                    document.getElementById("characterTableContainer").innerHTML = data;
                })
                .catch(error => console.error("Error loading characters:", error));
        });
    }

    // Initialize word cloud if data exists
    const characterDataElement = document.getElementById("characterData");
    if (characterDataElement && !searchPerformed)
    {
        // Check if WordCloud is available
        if (typeof WordCloud === 'undefined')
        {
            console.error("WordCloud library not loaded");
            const container = document.getElementById("wordCloudContainer");
            if (container)
            {
                container.innerHTML =
                    '<div class="alert alert-warning">Word cloud library failed to load. Please refresh the page.</div>';
            }
            return;
        }

        try
        {
            const characterData = JSON.parse(characterDataElement.textContent);
            console.log("Character data loaded:", characterData.length, "characters");

            // Transform data for wordcloud2.js format: [[text, weight], ...]
            const wordCloudData = characterData.map(item => [item.text, item.weight]);
            console.log("Word cloud data prepared");

            // Store character IDs for click handling
            const characterMap = {};
            characterData.forEach(item =>
            {
                characterMap[item.text] = item.characterId;
            });

            const canvas = document.getElementById("characterCloud");
            if (!canvas)
            {
                console.error("Canvas element not found");
                return;
            }

            // Set canvas actual dimensions to match display size, accounting for device pixel ratio
            const container = canvas.parentElement;
            const dpr = window.devicePixelRatio || 1;
            const displayWidth = container.offsetWidth;
            const displayHeight = container.offsetHeight;

            // Set canvas resolution (accounting for high-DPI displays)
            canvas.width = displayWidth * dpr;
            canvas.height = displayHeight * dpr;

            // Scale canvas context to match
            const ctx = canvas.getContext('2d');
            ctx.scale(dpr, dpr);

            console.log("Initializing word cloud with dimensions:", displayWidth, "x", displayHeight, "DPR:", dpr);

            // Initialize word cloud
            WordCloud(canvas, {
                list: wordCloudData,
                gridSize: Math.round(8 * dpr),  // Scale grid size for DPR
                weightFactor: function (size)
                {
                    // Adaptive scaling: find max size in dataset
                    const maxSize = Math.max(...wordCloudData.map(item => item[1]));
                    const minSize = Math.min(...wordCloudData.map(item => item[1]));

                    // Normalize size to 0-1 range, then scale appropriately
                    const normalized = (size - minSize) / (maxSize - minSize);

                    // Map to font size range (e.g., 12px to 60px)
                    const minFontSize = 16;
                    const maxFontSize = 96;
                    return minFontSize + (normalized * (maxFontSize - minFontSize));
                },
                fontFamily: 'Arial, sans-serif',
                color: function (word, weight)
                {
                    // Color based on weight (blue shades)
                    const intensity = Math.min(weight / 30 * 100, 100);
                    return `hsl(210, ${50 + intensity / 2}%, ${60 - intensity / 3}%)`;
                },
                rotateRatio: 0.2,  // 20% of words rotated
                rotationSteps: 2,   // Only rotate 0° or 90°
                backgroundColor: '#ffffff',
                hover: function (item)
                {
                    if (item)
                    {
                        canvas.style.cursor = 'pointer';
                    } else
                    {
                        canvas.style.cursor = 'default';
                    }
                },
                click: function (item)
                {
                    if (item && item[0])
                    {
                        const characterName = item[0];
                        const characterId = characterMap[characterName];
                        if (characterId)
                        {
                            window.location.href = `/Character/CharacterDetail?characterId=${characterId}`;
                        }
                    }
                }
            });

            console.log("Word cloud initialized successfully");

            // Handle window resize
            let resizeTimeout;
            window.addEventListener('resize', function ()
            {
                clearTimeout(resizeTimeout);
                resizeTimeout = setTimeout(function ()
                {
                    // Re-initialize word cloud with new dimensions
                    const container = canvas.parentElement;
                    const dpr = window.devicePixelRatio || 1;
                    const displayWidth = container.offsetWidth;
                    const displayHeight = container.offsetHeight;

                    canvas.width = displayWidth * dpr;
                    canvas.height = displayHeight * dpr;

                    const ctx = canvas.getContext('2d');
                    ctx.scale(dpr, dpr);

                    WordCloud(canvas, {
                        list: wordCloudData,
                        gridSize: Math.round(8 * dpr),
                        weightFactor: function (size)
                        {
                            // Adaptive scaling
                            const maxSize = Math.max(...wordCloudData.map(item => item[1]));
                            const minSize = Math.min(...wordCloudData.map(item => item[1]));
                            const normalized = (size - minSize) / (maxSize - minSize);
                            const minFontSize = 12;
                            const maxFontSize = 60;
                            return minFontSize + (normalized * (maxFontSize - minFontSize));
                        },
                        fontFamily: 'Arial, sans-serif',
                        color: function (word, weight)
                        {
                            const intensity = Math.min(weight / 30 * 100, 100);
                            return `hsl(210, ${50 + intensity / 2}%, ${60 - intensity / 3}%)`;
                        },
                        rotateRatio: 0.2,
                        rotationSteps: 2,
                        backgroundColor: '#ffffff',
                        hover: function (item)
                        {
                            canvas.style.cursor = item ? 'pointer' : 'default';
                        },
                        click: function (item)
                        {
                            if (item && item[0])
                            {
                                const characterId = characterMap[item[0]];
                                if (characterId)
                                {
                                    window.location.href = `/Character/CharacterDetail?characterId=${characterId}`;
                                }
                            }
                        }
                    });
                }, 250);
            });

        } catch (error)
        {
            console.error("Error initializing word cloud:", error);
            const container = document.getElementById("wordCloudContainer");
            if (container)
            {
                container.innerHTML =
                    '<div class="alert alert-danger">Error creating word cloud: ' + error.message + '</div>';
            }
        }
    }
});