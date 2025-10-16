async function getResults(jobId) {
    let table = document.getElementById("job-results-table")

    let response = null
    try {
        response = await send(`/api/jobs/${jobId}`, "GET", null, getCsrfTokenHeader())
        response = await response.json();
    } catch (error) {
        let text = await response.text();
        alert(text);
        return
    }

    let row = table.insertRow(1);
    var statusCell = row.insertCell(0);
    var startedCell = row.insertCell(1);
    var finishedCell = row.insertCell(2);
    var resultsCell = row.insertCell(3);

    if (response.results != null) {
        saveBase64File(response.results, "results.zip", "application/zip")
    }

    statusCell.innerHTML = response.status;
    startedCell.innerHTML = response.startedAt == null ? "not started" : response.startedat;
    finishedCell.innerHTML = response.finishedAt ? "not finished" : response.finishedat;
    resultsCell.innerHTML = response.results == null ? "not found" : "saved to results.zip";
}

function saveBase64File(base64Data, filename, mimeType) {
    const linkSource = `data:${mimeType};base64,${base64Data}`;
    const downloadLink = document.createElement('a');

    downloadLink.href = linkSource;
    downloadLink.download = filename;

    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}
