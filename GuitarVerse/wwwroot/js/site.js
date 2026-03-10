// ------------------------------
// PASSWORD SHOW/HIDE
// ------------------------------
document.querySelectorAll('input[type="password"][data-show-toggle]').forEach(passwordField => {
    const checkboxId = passwordField.getAttribute('data-show-toggle');
    const toggleCheckbox = document.getElementById(checkboxId);

    if (toggleCheckbox) {
        toggleCheckbox.addEventListener('change', () => {
            const show = toggleCheckbox.checked;

            // за регистрация и reset password
            document.querySelectorAll(`input[data-show-toggle="${checkboxId}"]`).forEach(pf => {
                pf.type = show ? 'text' : 'password';
            });
        });
    }
});


// ------------------------------
// PASSWORD VALIDATION (universal)
// ------------------------------
function setupPasswordValidation(passwordSelector, confirmSelector, submitSelector, criteriaSelector) {
    const password = document.querySelector(passwordSelector);
    const confirmPassword = document.querySelector(confirmSelector);
    const submitBtn = document.querySelector(submitSelector);
    const criteriaText = document.querySelector(criteriaSelector);

    if (!password || !confirmPassword || !criteriaText) return;

    function validatePassword() {
        const value = password.value;
        const hasUppercase = /[A-Z]/.test(value);
        const hasNumber = /[0-9]/.test(value);
        const hasLength = value.length >= 8;
        const passwordsMatch = password.value === confirmPassword.value;

        if (submitBtn) {
            submitBtn.disabled = !(hasUppercase && hasNumber && hasLength && passwordsMatch);
        }

        if (hasUppercase && hasNumber && hasLength && passwordsMatch) {
            criteriaText.classList.remove("text-danger");
            criteriaText.classList.add("text-success");
            criteriaText.textContent = "Password meets all requirements.";
        } else {
            criteriaText.classList.remove("text-success");
            criteriaText.classList.add("text-danger");

            if (!passwordsMatch) {
                criteriaText.textContent = "Passwords do not match.";
            } else {
                criteriaText.textContent =
                    "Password must be at least 8 characters, include one uppercase letter and one number.";
            }
        }
    }

    password.addEventListener("input", validatePassword);
    confirmPassword.addEventListener("input", validatePassword);
}

// ------------------------------
// Initialize validation for registration
// ------------------------------
setupPasswordValidation(
    "#password",          // password input in registration form
    "#confirmPassword",   // confirm input in registration form
    "#registerButton",    // submit button in registration form
    "#passwordCriteria"   // criteria div in registration form
);

// ------------------------------
// Initialize validation for Reset Password
// ------------------------------
setupPasswordValidation(
    "#Password",          // password input in reset form
    "#ConfirmPassword",   // confirm input in reset form
    "form#resetPasswordForm button[type='submit']", // submit button
    "#passwordCriteria"   // criteria div in reset form
);
