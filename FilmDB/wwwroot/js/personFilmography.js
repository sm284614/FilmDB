function filterTable(jobTitle)
{
	const rows = document.querySelectorAll('.film-row');
	rows.forEach(row =>
	{
		if (jobTitle === 'all' || row.getAttribute('data-job') === jobTitle)
		{
			row.style.display = ''; // Show row
		}
		else
		{
			row.style.display = 'none'; // Hide row
		}
	});
}