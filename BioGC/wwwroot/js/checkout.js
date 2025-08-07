document.addEventListener('DOMContentLoaded', function () {
    const summaryList = document.getElementById('order-summary-list');
    const summarySubtotal = document.getElementById('summary-subtotal');
    const summaryShipping = document.getElementById('summary-shipping');
    const summaryTotal = document.getElementById('summary-total');
    const placeOrderBtn = document.getElementById('place-order-btn');
    const checkoutForm = document.getElementById('checkout-form');
    const errorDiv = document.getElementById('checkout-error');
    const spinner = placeOrderBtn?.querySelector('.spinner-border');
    const shippingZoneSelect = document.getElementById('ShippingZoneId');

    // 1. Load the raw cart data from localStorage.
    const rawCart = JSON.parse(localStorage.getItem('cart')) || [];

    // 2. Create a new, clean version of the cart to ensure data types are correct.
    const cart = rawCart.map(item => {
        const priceString = String(item.price).replace(/[^0-9.]/g, '');
        return {
            ...item,
            price: parseFloat(priceString) || 0,
            quantity: parseInt(item.quantity, 10) || 0
        };
    });

    let shippingZones = [];

    if (cart.length === 0 && window.location.pathname.toLowerCase().includes('checkout')) {
        window.location.href = '/Home/cart';
        return;
    }

    if (shippingZoneSelect) {
        // 3. Populate the shippingZones array from the HTML select options.
        Array.from(shippingZoneSelect.options).forEach(option => {
            if (option.value) {
                const costMatch = option.text.match(/\(\+\$(\d+\.\d+)\)/);
                if (costMatch) {
                    shippingZones.push({
                        id: parseInt(option.value),
                        cost: parseFloat(costMatch[1])
                    });
                }
            }
        });
        // **THE FIX IS HERE:** Attach the event listener to update summary on change.
        shippingZoneSelect.addEventListener('change', renderAndCalculateSummary);
    }

    function renderAndCalculateSummary() {
        if (!summaryList) return;
        summaryList.innerHTML = '';
        let subtotal = 0;
        const lang = document.documentElement.lang || 'en';

        cart.forEach(item => {
            if (item.price > 0 && item.quantity > 0) {
                subtotal += item.price * item.quantity;
                const itemName = lang === 'ar' ? item.nameAr : item.nameEn;
                summaryList.innerHTML += `<div class="d-flex justify-content-between mb-2"><span>${item.quantity} x ${itemName}</span><span>$${(item.price * item.quantity).toFixed(2)}</span></div>`;
            }
        });

        const selectedZoneId = shippingZoneSelect ? parseInt(shippingZoneSelect.value) : 0;
        const selectedZone = shippingZones.find(z => z.id === selectedZoneId);
        const shippingCost = selectedZone ? selectedZone.cost : 0;

        summarySubtotal.textContent = `$${subtotal.toFixed(2)}`;
        summaryShipping.textContent = `$${shippingCost.toFixed(2)}`;
        summaryTotal.textContent = `$${(subtotal + shippingCost).toFixed(2)}`;
    }

    if (placeOrderBtn) {
        placeOrderBtn.addEventListener('click', async function () {
            setLoading(true);
            if (!validateForm()) {
                setLoading(false);
                return;
            }

            const payload = {
                shippingAddress: document.getElementById('ShippingAddress').value,
                shippingZoneId: parseInt(document.getElementById('ShippingZoneId').value),
                cartItems: cart.map(item => ({
                    id: parseInt(item.id),
                    quantity: item.quantity,
                    nameEn: item.nameEn,
                    price: item.price
                }))
            };

            try {
                const response = await fetch('/Checkout/CreateCheckoutSession', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();
                if (!response.ok) {
                    throw new Error(result.error || 'Failed to create checkout session.');
                }

                const stripe = Stripe(checkoutForm.dataset.publishableKey);
                const { error } = await stripe.redirectToCheckout({ sessionId: result.sessionId });
                if (error) throw new Error(error.message);

            } catch (error) {
                showError(error.message);
                setLoading(false);
            }
        });
    }

    function setLoading(isLoading) {
        if (!placeOrderBtn || !spinner) return;
        placeOrderBtn.disabled = isLoading;
        spinner.style.display = isLoading ? 'inline-block' : 'none';
    }

    function showError(message) {
        if (!errorDiv) return;
        errorDiv.textContent = message;
        errorDiv.style.display = 'block';
    }

    function hideError() {
        if (!errorDiv) return;
        errorDiv.style.display = 'none';
    }

    function validateForm() {
        hideError();
        let isValid = true;
        const requiredFields = ['FullName', 'PhoneNumber', 'ShippingAddress', 'ShippingZoneId'];
        requiredFields.forEach(id => {
            const input = document.getElementById(id);
            if (input && !input.value.trim()) {
                input.classList.add('is-invalid');
                showError('Please fill out all required fields, including shipping zone.');
                isValid = false;
            } else if (input) {
                input.classList.remove('is-invalid');
            }
        });
        return isValid;
    }

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    renderAndCalculateSummary();
});
