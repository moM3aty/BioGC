document.addEventListener('DOMContentLoaded', function () {
    const loginTab = document.getElementById("loginTab");
    const registerTab = document.getElementById("registerTab");
    const loginForm = document.getElementById("loginForm");
    const registerForm = document.getElementById("registerForm");


    if (registerForm && registerForm.classList.contains('active')) {
        loginTab?.classList.remove("active");
        registerTab?.classList.add("active");
        loginForm?.classList.remove("active");
    } else {
        loginTab?.classList.add("active");
        registerTab?.classList.remove("active");
        loginForm?.classList.add("active");
        registerForm?.classList.remove("active");
    }

    loginTab?.addEventListener("click", (e) => {
        e.preventDefault();
        loginTab.classList.add("active");
        registerTab.classList.remove("active");
        loginForm.classList.add("active");
        registerForm.classList.remove("active");
    });

    registerTab?.addEventListener("click", (e) => {
        e.preventDefault();
        registerTab.classList.add("active");
        loginTab.classList.remove("active");
        registerForm.classList.add("active");
        loginForm.classList.remove("active");
    });

    document.body.addEventListener("click", (e) => {
        if (e.target.classList.contains("toggle-password")) {
            const inputId = e.target.getAttribute("data-input");
            const input = document.getElementById(inputId);
            if (input) {
                input.type = input.type === "password" ? "text" : "password";
                e.target.classList.toggle("fa-eye");
                e.target.classList.toggle("fa-eye-slash");
            }
        }
    });
});
