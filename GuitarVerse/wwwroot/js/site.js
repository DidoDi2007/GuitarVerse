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



// ------------------------------
// Зареждане на продуктите
// ------------------------------
document.addEventListener("DOMContentLoaded", function () {

    // 1. ВАЖНО: Проверка дали сме на страницата Shop
    const form = document.getElementById("filterForm");
    if (!form) return; // Ако няма форма, спираме (за да не гърми на Home/Login)

    console.log("Shop Logic Active!");

    const gridContainer = document.getElementById("productGridContainer");
    const searchInput = document.getElementById("searchInput");
    const sortSelect = document.getElementById("sortOrder");

    // Функция за създаване на HTML карта
    function buildProductCard(product) {
        const formattedPrice = new Intl.NumberFormat('en-US', {
            style: 'decimal',
            minimumFractionDigits: 0
        }).format(product.price);

        // --- НОВА ЛОГИКА ЗА СТАТУСА ---
        let stockHtml = '';
        if (product.stock > 0) {
            stockHtml = '<div class="product-meta mt-1 text-success">In Stock</div>';
        } else {
            stockHtml = '<div class="product-meta mt-1 text-danger">Out of Stock</div>';
        }
        // ------------------------------

        // ВАЖНО: product.productID (или productId) трябва да идва правилно от контролера
        return `
            <div class="col-6 col-md-4 col-lg-3">
                <!-- Добавено position-relative -->
                <div class="card shop-card h-100 position-relative">
                    <div class="shop-img-box">
                        <img src="${product.imagePath}" alt="${product.name}">
                    </div>
                    <div class="card-body px-0 pt-2 text-center">
                        <div class="product-meta mb-1">${product.brand}</div>
                        <div class="product-title px-2">
                            <!-- Добавено stretched-link: Това прави цялата карта кликаема -->
                            <a href="/Shop/Details/${product.productID}" class="text-dark text-decoration-none stretched-link">
                                ${product.name}
                            </a>
                        </div>
                        <div class="product-price">${formattedPrice} €</div>
                         ${stockHtml}
                    </div>
                </div>
            </div>
        `;
    }

    function updateProducts() {
        const formData = new FormData(form);
        const searchParams = new URLSearchParams(formData).toString();

        const newUrl = window.location.pathname + "?" + searchParams;
        window.history.pushState(null, "", newUrl);

        fetch(newUrl, {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(response => response.json())
            .then(data => {
                gridContainer.innerHTML = "";

                if (data.length === 0) {
                    gridContainer.innerHTML = `
                    <div class="text-center py-5 text-white">
                        <h4>No products match your selection.</h4>
                        <p class="text-muted">Try clearing some filters.</p>
                    </div>`;
                } else {
                    let htmlBuffer = '<div class="row g-4">';
                    data.forEach(product => {
                        htmlBuffer += buildProductCard(product);
                    });
                    htmlBuffer += '</div>';
                    gridContainer.innerHTML = htmlBuffer;
                }
            })
            .catch(error => console.error("Error:", error));
    }

    // Слушатели
    form.addEventListener("change", function (e) {
        if (e.target.classList.contains("auto-submit") || e.target.classList.contains("auto-input")) {
            updateProducts();
        }
    });

    if (searchInput) {
        let debounceTimer;
        searchInput.addEventListener("input", () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(updateProducts, 500);
        });
        searchInput.addEventListener("keypress", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                updateProducts();
            }
        });
    }

    if (sortSelect) sortSelect.onchange = updateProducts;
});
//==========================
//Детайлна страница
//==========================


// Смяна на главната снимка при клик на тъмбнейл

    // Смяна на главната снимка + Active Class
    function changeImage(src, element) {
        document.getElementById('mainImage').src = src;

        // Махане на active от всички
        document.querySelectorAll('.thumbnail').forEach(el => el.classList.remove('active'));
    // Слагане на active на натиснатия
    element.classList.add('active');
}

//==========================
// Kolichka
//==========================


// Обновяване на баджа на количката
document.addEventListener("DOMContentLoaded", function () {
    fetch('/Cart/GetCartCount')
        .then(response => response.json())
        .then(count => {
            const badge = document.getElementById('cartBadge');
            if (count > 0) {
                badge.innerText = count;
                badge.style.display = 'inline-block'; // Показваме го само ако има продукти
            } else {
                badge.style.display = 'none';
            }
        })
        .catch(err => console.error("Error loading cart count", err));
});

// ------------------------------
// Initialize validation for Profile Security (Change Password)
// ------------------------------
setupPasswordValidation(
    "#newPassword",          // полето за новата парола
    "#confirmNewPassword",   // полето за потвърждение
    "#savePasswordBtn",      // бутонът за запис
    "#passwordCriteria"      // текстът с правилата
);



// ------------------------------
// Reviews
// ------------------------------
function updateCharCount(field) {
    document.getElementById('charCount').innerText = field.value.length + " /900";
}

// ------------------------------
// Бутон за проследяване на поръчката
// ------------------------------

function showTrackingDemo(trackingNum) {
    // 1. Слагаме номера в модала
    document.getElementById('modalTrackingNum').innerText = trackingNum;

    // 2. Показваме модала чрез Bootstrap
    var myModal = new bootstrap.Modal(document.getElementById('trackingModal'));
    myModal.show();
}


// ------------------------------
// Бутон за изтриване на профила
// ------------------------------



document.addEventListener("DOMContentLoaded", function () {
    const input = document.getElementById('deleteConfirmationInput');
    const btn = document.getElementById('finalDeleteBtn');

    input.addEventListener('input', function () {
        // Проверяваме дали въведеният текст е точно "DELETE"
        if (this.value === 'DELETE') {
            btn.disabled = false; // Отключваме бутона
            btn.classList.add('pulse-animation'); // Опционално: ефект
        } else {
            btn.disabled = true;  // Заключваме бутона
            btn.classList.remove('pulse-animation');
        }
    });
});
        