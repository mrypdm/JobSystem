// Requests
async function send(url, method, body, headers = {}, formBody = false) {
    if (body !== null && formBody != true) {
        body = JSON.stringify(body)
        headers["Content-Type"] = "application/json"
    }

    let response = await fetch(url, {
        method: method,
        headers: headers,
        body: body
    });

    if (response.ok) {
        return response
    }

    throw response;
}

function getCsrfTokenHeader() {
    return {
        "X-CSRF-TOKEN": $('input[name="__RequestVerificationToken"]').val()
    }
}

// Jobs
async function getResults(jobId) {
    let table = document.getElementById("job-results-table")

    let response = null
    try {
        response = await send(`/api/jobs/${jobId}`, "GET", null, getCsrfTokenHeader())
        response = await response.json();
    } catch (error) {
        let text = await error.text();
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
    finishedCell.innerHTML = response.finishedAt == null ? "not finished" : response.finishedat;
    resultsCell.innerHTML = response.results == null ? "not found" : "saved to results.zip";
}

async function createJob(form) {
    try {
        await send("/api/jobs/", "POST", new FormData(form), getCsrfTokenHeader(), formBody = true);
        location.replace("/");
    } catch (error) {
        let text = await error.text();
        alert(text);
    }
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

// Auth
async function login(form, returnUrl) {
    try {
        await send("/api/auth", "POST", new FormData(form), getCsrfTokenHeader(), formBody = true);
        location.replace(returnUrl);
    } catch (error) {
        let text = await error.text();
        alert(text);
    }
}

async function logout() {
    await send("/api/auth", "DELETE", null, getCsrfTokenHeader());
    location.replace("/auth/login");
}

function showPassword(passwordBoxId) {
    let passwordBox = document.getElementById(passwordBoxId);
    passwordBox.type = passwordBox.type === "password" ? "text" : "password";
}
