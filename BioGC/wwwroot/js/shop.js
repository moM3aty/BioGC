document.addEventListener('DOMContentLoaded', function () {
    const userIsLoggedIn = document.querySelector('a[href="/Account/Profile"]') !== null;

    /**
     * Renders the entire cart page content. This function should only run on the cart page.
     */
    function renderCartPage() {
        const container = document.getElementById('cart-items-container');

        // **FIX:** Only execute the rest of the code if the main cart container exists.
        // This prevents errors on other pages like Home, Details, etc.
        if (!container) {
            return;
        }

        const cart = JSON.parse(localStorage.getItem('cart')) || [];
        const emptyCartMsg = document.getElementById('empty-cart-msg');
        const cartSummarySection = document.getElementById('cart-summary-section');
        const lang = document.documentElement.lang || 'en';

        container.innerHTML = '';

        if (cart.length === 0) {
            if (emptyCartMsg) emptyCartMsg.style.display = 'block';
            if (cartSummarySection) cartSummarySection.style.display = 'none';
        } else {
            if (emptyCartMsg) emptyCartMsg.style.display = 'none';
            if (cartSummarySection) cartSummarySection.style.display = 'block';

            cart.forEach(item => {
                const itemElement = document.createElement('div');
                itemElement.className = 'cart-item-row d-flex align-items-center gap-3';
                itemElement.innerHTML = `
                    <img src="${item.image}" alt="${item.nameEn}" class="cart-product-image">
                    <div class="flex-grow-1">
                        <h5 class="mb-1">${lang === 'ar' ? item.nameAr : item.nameEn}</h5>
                        <span class="text-muted">$${parseFloat(item.price).toFixed(2)}</span>
                    </div>
                    <div class="quantity-selector">
                         <div class="input-group">
                            <button style="border-radius:25px;"  class="btn btn-outline-secondary btn-sm" type="button" onclick="changeQuantity('${item.id}', -1)">-</button>
                            <input  style="border-radius:25px;" type="text" class="form-control text-center" value="${item.quantity}" readonly>
                            <button style="border-radius:25px;"  class="btn btn-outline-secondary btn-sm" type="button" onclick="changeQuantity('${item.id}', 1)">+</button>
                        </div>
                    </div>
                    <div>
                        <strong class="me-3">$${(item.price * item.quantity).toFixed(2)}</strong>
                        <button class="btn btn-sm btn-outline-danger" onclick="removeFromCart('${item.id}')">&times;</button>
                    </div>
                `;
                container.appendChild(itemElement);
            });
            updateCartSummary();
            checkLoginStatus();
        }
    }

    /**
     * Updates the subtotal and total in the cart summary section.
     */
    function updateCartSummary() {
        const cart = JSON.parse(localStorage.getItem('cart')) || [];
        const total = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
        const subtotalEl = document.getElementById('cart-subtotal');
        const totalEl = document.getElementById('cart-total');
        if (subtotalEl) subtotalEl.textContent = `$${total.toFixed(2)}`;
        if (totalEl) totalEl.textContent = `$${total.toFixed(2)}`;
    }

    /**
     * Enables or disables the checkout button based on login status.
     */
    function checkLoginStatus() {
        const checkoutBtn = document.getElementById('checkout-btn');
        const loginPrompt = document.getElementById('login-prompt');
        if (!checkoutBtn || !loginPrompt) return;

        if (userIsLoggedIn) {
            checkoutBtn.classList.remove('disabled');
            checkoutBtn.style.pointerEvents = 'auto';
            loginPrompt.style.display = 'none';
        } else {
            checkoutBtn.classList.add('disabled');
            checkoutBtn.style.pointerEvents = 'none';
            loginPrompt.style.display = 'block';
        }
    }

    // Make these functions globally accessible from the inline onclick attributes on the cart page.
    window.changeQuantity = function (productId, amount) {
        let cart = JSON.parse(localStorage.getItem('cart')) || [];
        const item = cart.find(i => i.id === productId);
        if (item) {
            item.quantity += amount;
            if (item.quantity <= 0) {
                cart = cart.filter(i => i.id !== productId);
            }
        }
        localStorage.setItem('cart', JSON.stringify(cart));
        renderCartPage();
        if (typeof updateCartCount === 'function') {
            updateCartCount(); // This function is in main.js
        }
    }

    window.removeFromCart = function (productId) {
        let cart = JSON.parse(localStorage.getItem('cart')) || [];
        cart = cart.filter(i => i.id !== productId);
        localStorage.setItem('cart', JSON.stringify(cart));
        renderCartPage();
        if (typeof updateCartCount === 'function') {
            updateCartCount(); // This function is in main.js
        }
    }

    // Initial render call for the cart page.
    renderCartPage();
});
