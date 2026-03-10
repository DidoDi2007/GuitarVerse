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

    // 1. Слушател за Checkbox и Radio (те остават на "change", защото са мигновени)
    form.addEventListener("change", function (e) {
        if (e.target.classList.contains("auto-submit")) {
            updateProducts();
        }
    });

    // 2. Слушател за Цената (auto-input) - работи докато пишеш (input събитие)
    let priceTimer;
    document.querySelectorAll(".auto-input").forEach(input => {
        input.addEventListener("input", () => {
            clearTimeout(priceTimer);
            // Чакаме 600 милисекунди след последното натискане на клавиш, преди да филтрираме
            priceTimer = setTimeout(updateProducts, 600);
        });
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



// ------------------------------
// Нотификация да се влезе в профила
// ------------------------------

document.addEventListener("DOMContentLoaded", function () {
    // Проверяваме дали вече сме го показвали в тази сесия (за да не досаждаме)
    if (!sessionStorage.getItem("welcomeToastShown")) {
        var toastEl = document.getElementById('loginToast');
        var toast = new bootstrap.Toast(toastEl, { delay: 10000 }); // Скрива се след 10 сек
        toast.show();

        // Записваме, че е показан
        sessionStorage.setItem("welcomeToastShown", "true");
    }
});





// ------------------------------
// Сравнение на продукти
// ------------------------------


// 1. Добавяне
function addToCompare(productId) {
    fetch('/Compare/Add/' + productId, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateCompareBanner();
            } else {
                alert(data.message);
            }
        });
}

// 2. Изтриване на всички
function clearCompare() {
    fetch('/Compare/ClearAll', { method: 'POST' })
        .then(() => updateCompareBanner());
}

// 3. Изтриване на ЕДИН продукт
function removeCompareItem(productId) {
    fetch('/Compare/RemoveItem/' + productId, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) updateCompareBanner();
        });
}

// 4. Ръчно затваряне на прозореца
function closeComparePopup() {
    document.getElementById('comparePopup').style.display = 'none';
}

// 5. Обновяване на интерфейса
function updateCompareBanner() {

    // --- НОВО: ПРОВЕРКА ЗА ТЕКУЩАТА СТРАНИЦА ---
    // Ако сме в страницата /Compare, изобщо не изпълняваме функцията и скриваме прозореца.
    if (window.location.pathname.toLowerCase().includes('/compare')) {
        const popup = document.getElementById('comparePopup');
        if (popup) popup.style.display = 'none';
        return; // Спираме изпълнението
    }
    // -------------------------------------------

    fetch('/Compare/GetCompareData')
        .then(response => response.json())
        .then(products => {
            const popup = document.getElementById('comparePopup');
            const container = document.getElementById('compareItemsContainer');
            const btnGo = document.getElementById('btnGoCompare');
            const badge = document.getElementById('compareBadge');

            if (products && products.length > 0) {
                popup.style.display = 'block'; // Показваме прозореца
                container.innerHTML = '';

                // Обновяваме бройката в червеното кръгче
                badge.innerText = products.length;

                products.forEach(p => {
                    container.innerHTML += `
                    <div class="compare-item" title="${p.brand} ${p.name}">
                        <div class="compare-item-img-box">
                            <img src="${p.imagePath}" alt="${p.name}">
                            <!-- Бутонче за триене на този конкретен продукт -->
                            <button class="compare-item-remove" onclick="removeCompareItem(${p.productID})">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                        <div class="compare-item-name">${p.brand}<br>${p.name}</div>
                    </div>
                `;
                });

                // Отключваме бутона "Compare" само ако има поне 2 продукта
                if (products.length >= 2) {
                    btnGo.classList.remove('disabled');
                } else {
                    btnGo.classList.add('disabled');
                }
            } else {
                popup.style.display = 'none'; // Скриваме го, ако е празно
            }
        });
}

document.addEventListener("DOMContentLoaded", updateCompareBanner);


// ==========================
// СРАВНЕНИЕ НА ПРОДУКТИ: SHOW MORE / SHOW LESS
// ==========================
document.addEventListener('DOMContentLoaded', function () {
    const btn = document.getElementById('toggleSpecsBtn');

    // Проверяваме дали бутонът съществува на текущата страница
    if (btn) {
        btn.addEventListener('click', function () {
            // Намираме всички скрити редове
            const hiddenRows = document.querySelectorAll('.extra-spec-row');
            let isShowing = false;

            hiddenRows.forEach(row => {
                row.classList.toggle('show-specs');
                if (row.classList.contains('show-specs')) {
                    isShowing = true;
                }
            });

            // Сменяме текста и иконата
            if (isShowing) {
                btn.innerHTML = 'Hide Detailed Specs <i class="fa-solid fa-chevron-up ms-2"></i>';
            } else {
                btn.innerHTML = 'Show Detailed Specs <i class="fa-solid fa-chevron-down ms-2"></i>';
            }
        });
    }
});