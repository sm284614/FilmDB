document.addEventListener("DOMContentLoaded", function ()
{
	document.getElementById("characterSearchForm").addEventListener("submit", function (event)
	{
		event.preventDefault(); // Prevent full page reload
		document.getElementById("characterTableContainer").innerHTML = "Loading data...";
		const query = document.getElementById("characterSearchQuery").value;
		fetch(`/Character/CharacterSearch?query=${encodeURIComponent(query)}`)
			.then(response => response.text())
			.then(data =>
			{
				document.getElementById("characterTableContainer").innerHTML = data;
			})
			.catch(error => console.error("Error loading characters:", error));
	});
});