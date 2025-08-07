// --- State Management ---
let currentLanguage = getCookie("language") || 'en';

const translations = {
    en: { direction: 'ltr' },
    ar: { direction: 'rtl' }
};

// --- Validation Data ---
const validationMessages = {
    required: {
        en: "This field cannot be empty",
        ar: "لا يمكن ترك هذا الحقل فارغًا"
    },
    name: {
        en: "Please enter a valid name (letters only)",
        ar: "يرجى إدخال اسم صالح (أحرف فقط)"
    },
    email: {
        en: "Please enter a valid email address",
        ar: "يرجى إدخال بريد إلكتروني صالح"
    },
    username: {
        en: "Invalid username (at least 3 characters)",
        ar: "اسم المستخدم غير صالح (٣ أحرف على الأقل)"
    },
    password: {
        en: "Password must be at least 6 characters",
        ar: "كلمة المرور يجب أن تكون 6 أحرف على الأقل"
    },
    confirmPassword: {
        en: "Passwords do not match",
        ar: "كلمتا المرور غير متطابقتين"
    },
    phone: {
        en: "Enter a valid phone number",
        ar: "أدخل رقم هاتف صحيح"
    }
};

const validationPatterns = {
    name: /^[A-Za-z؀-ۿ ]{2,}$/,
    email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    username: /^[a-zA-Z0-9_\u0621-\u064A]{3,}$/,
    password: /^.{6,}$/,
    phone: /^[0-9]{10,15}$/
};

// --- Core UI & Language Functions ---

function setLanguage(lang) {
    currentLanguage = lang;
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === "ar" ? "rtl" : "ltr";
    document.cookie = `language=${lang};path=/;max-age=31536000`; // Expires in 1 year
    translatePageContent(lang);
    const langToggle = document.getElementById("langToggle");
    if (langToggle) {
        langToggle.textContent = lang === "ar" ? "English" : "العربية";
    }
    if (typeof initializeSwiper === 'function') {
        initializeSwiper();
    }
}

function toggleLanguage() {
    const newLang = currentLanguage === "en" ? "ar" : "en";
    setLanguage(newLang);
}

function translatePageContent(lang) {
    document.querySelectorAll('[data-en], [data-ar]').forEach(el => {
        const text = el.getAttribute(`data-${lang}`);
        if (text !== null) el.textContent = text;
    });
    document.querySelectorAll('[data-en-placeholder], [data-ar-placeholder]').forEach(input => {
        const placeholder = input.getAttribute(`data-${lang}-placeholder`);
        if (placeholder !== null) input.setAttribute('placeholder', placeholder);
    });
}

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}

// --- Validation Functions ---

function showError(input, messageKey) {
    removeError(input);
    const message = validationMessages[messageKey]?.[currentLanguage] || "Invalid input";
    const errorDiv = document.createElement("div");
    errorDiv.className = "text-danger mt-1 small error-message";
    errorDiv.textContent = message;
    input.classList.add("is-invalid");
    input.parentElement.appendChild(errorDiv);
}

function removeError(input) {
    input.classList.remove("is-invalid");
    const error = input.parentElement.querySelector(".error-message");
    if (error) {
        error.remove();
    }
}

function validateInput(input) {
    const name = input.name;
    const value = input.value.trim();

    if (value === "" && input.hasAttribute('required')) {
        showError(input, "required");
        return false;
    }

    const patternKey = name.toLowerCase().replace('confirm', '').replace('fullname', 'name');
    if (value !== "" && validationPatterns[patternKey] && !validationPatterns[patternKey].test(value)) {
        showError(input, patternKey);
        return false;
    }

    if (name.toLowerCase().includes("confirmpassword")) {
        const form = input.closest('form');
        const passwordInput = form.querySelector('[name*="Password"], [name*="password"]');
        if (passwordInput && value !== passwordInput.value.trim()) {
            showError(input, "confirmPassword");
            return false;
        }
    }

    removeError(input);
    return true;
}

// --- E-commerce Functions ---

function updateCartCount() {
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    const totalItems = cart.reduce((sum, item) => sum + (item.quantity || 0), 0);
    const cartCountSpan = document.getElementById('cart-count');
    if (cartCountSpan) {
        cartCountSpan.textContent = totalItems;
        cartCountSpan.style.display = totalItems > 0 ? 'inline-block' : 'none';
    }
}

function addToCart(product) {
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    let existingItem = cart.find(item => item.id === product.id);

    if (existingItem) {
        existingItem.quantity += product.quantity || 1;
    } else {
        cart.push({ ...product, quantity: product.quantity || 1 });
    }

    localStorage.setItem('cart', JSON.stringify(cart));
    updateCartCount();
    showToast(currentLanguage === 'ar' ? `تمت إضافة "${product.nameAr}" للسلة` : `Added "${product.nameEn}" to cart`);
}

async function toggleFavorite(productId, buttonElement) {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) {
        showToast(currentLanguage === 'ar' ? 'يرجى تسجيل الدخول أولاً' : 'Please log in first');
        return;
    }

    try {
        const response = await fetch(`/api/Wishlist/ToggleFavorite/${productId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': tokenInput.value,
                'Content-Type': 'application/json'
            }
        });

        if (response.status === 401) {
            showToast(currentLanguage === 'ar' ? 'يرجى تسجيل الدخول أولاً' : 'Please log in first');
            return;
        }

        if (response.ok) {
            const result = await response.json();
            let toastMessage = '';
            if (result.status === 'added') {
                buttonElement.classList.add('active');
                toastMessage = currentLanguage === 'ar' ? 'تمت الإضافة للمفضلة' : 'Added to favorites';
            } else {
                buttonElement.classList.remove('active');
                toastMessage = currentLanguage === 'ar' ? 'تمت الإزالة من المفضلة' : 'Removed from favorites';
            }
            showToast(toastMessage);
        } else {
            showToast(currentLanguage === 'ar' ? 'حدث خطأ ما' : 'Something went wrong');
        }
    } catch (error) {
        console.error('Error toggling favorite:', error);
    }
}

function getProductDataFromCard(cardElement) {
    return {
        id: cardElement.dataset.productId,
        nameEn: cardElement.dataset.productNameEn,
        nameAr: cardElement.dataset.productNameAr,
        price: parseFloat(cardElement.dataset.price),
        image: cardElement.dataset.image,
        quantity: 1
    };
}

// --- Utility Functions ---
function showToast(message) {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = message;
    container.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 100);
    setTimeout(() => {
        toast.classList.remove('show');
        toast.addEventListener('transitionend', () => toast.remove());
    }, 3000);
}

async function initializeFavoriteButtonsState() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) return; // User is not logged in

    try {
        const response = await fetch('/api/Wishlist/MyFavorites');
        if (response.ok) {
            const favoriteIds = await response.json();
            document.querySelectorAll('.btn-favorite').forEach(btn => {
                const card = btn.closest('[data-product-id]');
                if (card) {
                    const productId = parseInt(card.dataset.productId, 10);
                    if (favoriteIds.includes(productId)) {
                        btn.classList.add('active');
                    } else {
                        btn.classList.remove('active');
                    }
                }
            });
        }
    } catch (error) {
        console.error("Could not fetch user's favorites:", error);
    }
}

// --- Component Initializers ---
let swiperInstance;
function initializeSwiper() {
    if (typeof Swiper !== 'undefined' && document.querySelector(".mySwiper")) {
        if (swiperInstance) {
            try { swiperInstance.destroy(true, true); } catch (e) { console.error("Error destroying swiper:", e) }
        }
        swiperInstance = new Swiper(".mySwiper", {
            loop: true,
            grabCursor: true,
            autoplay: { delay: 3000, disableOnInteraction: false },
            navigation: { nextEl: ".swiper-button-next", prevEl: ".swiper-button-prev" },
            slidesPerView: 1,
            spaceBetween: 20,
            dir: currentLanguage === 'ar' ? 'rtl' : 'ltr',
            breakpoints: { 768: { slidesPerView: 2 }, 992: { slidesPerView: 3 } }
        });
    }
}
async function handleConsultationSubmit(e) {
    e.preventDefault();
    const form = e.target;
    // Basic validation
    const nameInput = form.querySelector("input[name='name']");
    const emailInput = form.querySelector("input[name='email']");
    if (nameInput.value.trim() === "" || emailInput.value.trim() === "") {
        showToast(currentLanguage === 'ar' ? 'يرجى ملء الحقول المطلوبة' : 'Please fill in the required fields');
        return;
    }

    const formData = new FormData(form);
    const data = {
        fullName: formData.get('name'),
        email: formData.get('email'),
        request: formData.get('request'),
        message: formData.get('message'),
        service: form.dataset.service || 'General Inquiry'
    };
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    try {
        const response = await fetch('/Contact/SendMessage', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify(data),
            credentials: 'same-origin'
        });
        const result = await response.json();
        if (result.success) {
            const whatsAppNumber = "+201124743148"; 
            const selectElement = form.querySelector('select[name="request"]');
            const selectedOptionText = selectElement ? selectElement.options[selectElement.selectedIndex].text : data.request;
            let messageBody = `New Consultation Request:\n\n*Service:* ${data.service}\n*Full Name:* ${data.fullName}\n*Email:* ${data.email}\n*Interest:* ${selectedOptionText}\n*Message:* ${data.message}`;
            const encodedMessage = encodeURIComponent(messageBody);
            const whatsappUrl = `https://wa.me/${whatsAppNumber}?text=${encodedMessage}`;
            window.open(whatsappUrl, '_blank');
            showToast(currentLanguage === 'ar' ? 'تم إرسال طلبك بنجاح!' : 'Your request has been sent successfully!');
            form.reset();
        } else {
            showToast(currentLanguage === 'ar' ? 'حدث خطأ. يرجى المحاولة مرة أخرى.' : 'An error occurred. Please try again.');
        }
    } catch (error) {
        console.error('Form submission error:', error);
        showToast(currentLanguage === 'ar' ? 'فشل الاتصال بالخادم.' : 'Failed to connect to the server.');
    }
}

    function updateCartCount() {
        const cart = JSON.parse(localStorage.getItem('cart')) || [];

        const totalItems = cart.reduce((sum, item) => sum + (item.quantity || 0), 0);

        const cartCountSpan = document.getElementById('cart-count');

        if (cartCountSpan) {
            cartCountSpan.textContent = totalItems;
            cartCountSpan.style.display = totalItems > 0 ? 'inline-block' : 'none';
        }
    }

    
    function addToCart(product) {
        let cart = JSON.parse(localStorage.getItem('cart')) || [];

        let existingItem = cart.find(item => item.id === product.id);

        if (existingItem) {
            existingItem.quantity += product.quantity || 1;
        } else {
            cart.push({ ...product, quantity: product.quantity || 1 });
        }

        localStorage.setItem('cart', JSON.stringify(cart));

        updateCartCount();

        showToast(currentLanguage === 'ar' ? `تمت إضافة "${product.nameAr}" للسلة` : `Added "${product.nameEn}" to cart`);
    }

    document.body.addEventListener('click', function (e) {
        const cartBtn = e.target.closest('.btn-cart');
        if (cartBtn) {
            const productDataSource = cartBtn.closest('[data-product-id]');
            if (productDataSource) {
                const product = {
                    id: cartBtn.dataset.productId,
                    nameEn: cartBtn.dataset.productName,
                    nameAr: cartBtn.dataset.productNameAr || cartBtn.dataset.productName, 
                    price: parseFloat(cartBtn.dataset.productPrice),
                    image: cartBtn.dataset.productImage,
                    quantity: quantity
                };

                const quantityInput = document.getElementById('quantity');
                if (quantityInput && productDataSource.classList.contains('product-main-card')) {
                    product.quantity = parseInt(quantityInput.value, 10) || 1;
                }

                addToCart(product);
            }
        }
    });

    document.addEventListener('DOMContentLoaded', () => {
        updateCartCount();
    });


(function () {
    setLanguage(currentLanguage);
    updateCartCount();
    initializeSwiper();
    initializeFavoriteButtonsState();

    document.querySelectorAll('form.consultation-form').forEach(form => {
        form.addEventListener('submit', handleConsultationSubmit);
    });

    document.getElementById("langToggle")?.addEventListener("click", toggleLanguage);

    document.body.addEventListener('click', function (e) {
        const cartBtn = e.target.closest('.btn-cart');
        if (cartBtn) {
            const productCard = cartBtn.closest('[data-product-id]');
            if (productCard) {
                const product = getProductDataFromCard(productCard);
                addToCart(product);
            }
        }

        const favBtn = e.target.closest('.btn-favorite');
        if (favBtn) {
            const productCard = favBtn.closest('[data-product-id]');
            if (productCard) {
                const productId = productCard.dataset.productId;
                toggleFavorite(productId, favBtn);
            }
        }
    });
})();
