document.addEventListener("DOMContentLoaded", function ()
{
	document.querySelectorAll(".job-button").forEach(button =>
	{
		button.addEventListener("click", function ()
		{
			let jobId = this.dataset.id;
			// Remove highlight from all buttons
			document.querySelectorAll(".job-button").forEach(btn =>
			{ // FIXED HERE
				btn.classList.remove("btn-primary");
				btn.classList.add("btn-secondary"); // Reset all to default
			});
			this.classList.remove("btn-secondary"); // Reset color
			this.classList.add("btn-primary");

			// Show "Loading..." message while waiting for the AJAX request
			const container = document.getElementById("jobCountContainer");
			container.innerHTML = "<p>Loading...</p>";

			fetch(`/Person/JobCount?id=${jobId}`)
				.then(response => response.text())
				.then(data =>
				{
					container.innerHTML = data; // Insert response into container
				})
				.catch(error =>
				{
					container.innerHTML = "<p class='text-danger'>Error loading data.</p>";
				});
		});
	});
});