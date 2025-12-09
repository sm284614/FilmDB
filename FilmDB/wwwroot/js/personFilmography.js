// Wait for DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function ()
{
    // Get all filter buttons
    const filterButtons = document.querySelectorAll('.job-filter-btn');

    // Attach click event to each button
    filterButtons.forEach(button =>
    {
        button.addEventListener('click', function ()
        {
            const jobTitle = this.dataset.job;
            filterTable(jobTitle);
        });
    });
});

function filterTable(jobTitle)
{
    const rows = document.querySelectorAll('.film-row');
    rows.forEach(row =>
    {
        if (jobTitle === 'all' || row.dataset.job === jobTitle)
        {
            row.style.display = ''; // Show row
        } else
        {
            row.style.display = 'none'; // Hide row
        }
    });
}