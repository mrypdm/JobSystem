async function logout() {
    await send("/api/auth", "DELETE", null, getCsrfTokenHeader());
    location.replace("/auth/login");
}

async function login(redirect) {
    let usernameBox = document.getElementById("username");
    let passwordBox = document.getElementById("password");
    let data = {
        "Username": usernameBox.value,
        "Password": passwordBox.value,
    }

    try {
        await send("/api/auth", "POST", data, getCsrfTokenHeader());
        location.replace(redirect)
    } catch (response) {
        let text = await response.text()
        alert(text);
    }
}

function showPassword(passwordBoxId) {
    let passwordBox = document.getElementById(passwordBoxId);
    if (passwordBox.type === "password") {
        passwordBox.type = "text";
    } else {
        passwordBox.type = "password";
    }
}
