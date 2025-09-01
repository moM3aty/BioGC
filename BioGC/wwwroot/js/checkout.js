document.addEventListener('DOMContentLoaded', function () {
    // --- GENERAL SETUP ---
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    const orderSummaryList = document.getElementById('order-summary-list');
    const summarySubtotal = document.getElementById('summary-subtotal');
    const summaryShipping = document.getElementById('summary-shipping');
    const summaryTotal = document.getElementById('summary-total');
    const shippingZoneSelect = document.getElementById('ShippingZoneId');
    const errorContainer = document.getElementById('checkout-error');
    const loader = document.getElementById('loader');
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // --- FUNCTIONS ---
    function updateSummary() {
        if (!orderSummaryList || !summarySubtotal || !summaryShipping || !summaryTotal) return;

        let subtotal = 0;
        orderSummaryList.innerHTML = ''; // Clear previous summary

        if (cart.length === 0) {
            orderSummaryList.innerHTML = `<p data-en="Your cart is empty." data-ar="سلتك فارغة.">Your cart is empty.</p>`;
            document.getElementById('paypal-button-container').style.display = 'none';
        } else {
            document.getElementById('paypal-button-container').style.display = 'block';
        }


        cart.forEach(item => {
            const itemTotal = item.price * item.quantity;
            subtotal += itemTotal;
            const lang = document.documentElement.lang || 'en';
            const itemName = lang === 'ar' ? item.nameAr : item.nameEn;

            orderSummaryList.innerHTML += `
                <div class="d-flex justify-content-between mb-2">
                    <span>${itemName} x ${item.quantity}</span>
                    <span>$${itemTotal.toFixed(2)}</span>
                </div>
            `;
        });

        summarySubtotal.textContent = `$${subtotal.toFixed(2)}`;

        let shippingCost = 0;
        const selectedZone = shippingZoneSelect.options[shippingZoneSelect.selectedIndex];
        if (selectedZone && selectedZone.value) {
            // Extract cost from text like "Zone Name (+$10.00)"
            const costMatch = selectedZone.text.match(/\(\+\$(\d+\.\d+)\)/);
            if (costMatch && costMatch[1]) {
                shippingCost = parseFloat(costMatch[1]);
            }
        }

        summaryShipping.textContent = `$${shippingCost.toFixed(2)}`;
        const total = subtotal + shippingCost;
        summaryTotal.textContent = `$${total.toFixed(2)}`;
    }

    // --- EVENT LISTENERS ---
    if (shippingZoneSelect) {
        shippingZoneSelect.addEventListener('change', updateSummary);
    }

    // --- PAYPAL BUTTONS RENDER ---
    if (cart.length > 0) {
        paypal.Buttons({
            // Sets up the transaction when a payment button is clicked
            createOrder: function (data, actions) {
                errorContainer.style.display = 'none';
                loader.style.display = 'block';

                const payload = {
                    shippingAddress: document.getElementById('ShippingAddress').value,
                    shippingZoneId: parseInt(shippingZoneSelect.value, 10) || 0,
                    cartItems: cart
                };

                return fetch('/Checkout/CreateOrder', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(payload)
                })
                    .then(response => {
                        if (!response.ok) {
                            return response.json().then(err => { throw new Error(err.error || 'Server error'); });
                        }
                        return response.json();
                    })
                    .then(data => {
                        loader.style.display = 'none';
                        return data.orderId;
                    })
                    .catch(err => {
                        loader.style.display = 'none';
                        errorContainer.textContent = `Error creating order: ${err.message}`;
                        errorContainer.style.display = 'block';
                        return null;
                    });
            },

            // Finalize the transaction after payer approval
            onApprove: function (data, actions) {
                loader.style.display = 'block';
                errorContainer.style.display = 'none';

                const payload = {
                    PayPalOrderId: data.orderID
                };

                return fetch('/Checkout/CaptureOrder', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(payload)
                })
                    .then(response => response.json())
                    .then(result => {
                        if (result.success) {
                            localStorage.removeItem('cart'); // Clear cart on success
                            window.location.href = '/Checkout/OrderConfirmation?orderId=' + result.orderId;
                        } else {
                            throw new Error(result.message || 'Payment capture failed.');
                        }
                    })
                    .catch(error => {
                        console.error('Capture Error:', error);
                        errorContainer.textContent = 'An error occurred while finalizing your payment. Please try again.';
                        errorContainer.style.display = 'block';
                        loader.style.display = 'none';
                    });
            },

            // Handle form validation before creating order
            onClick: function () {
                const fullName = document.getElementById('FullName').value;
                const phone = document.getElementById('PhoneNumber').value;
                const address = document.getElementById('ShippingAddress').value;
                const zone = shippingZoneSelect.value;

                if (!fullName || !phone || !address || !zone) {
                    errorContainer.textContent = 'Please fill out all required shipping details.';
                    errorContainer.style.display = 'block';
                    return false; // prevent order creation
                }
                errorContainer.style.display = 'none';
                return true; // proceed with order creation
            },

            // Handle errors from the PayPal button itself
            onError: function (err) {
                console.error('PayPal button error:', err);
                errorContainer.textContent = 'An unexpected error occurred with PayPal. Please try again.';
                errorContainer.style.display = 'block';
                loader.style.display = 'none';
            }
        }).render('#paypal-button-container');
    }

    // --- INITIAL LOAD ---
    updateSummary();
});

