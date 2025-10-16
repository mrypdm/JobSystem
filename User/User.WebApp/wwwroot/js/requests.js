async function send(url, method, body, headers = {}) {
    if (body !== null) {
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
