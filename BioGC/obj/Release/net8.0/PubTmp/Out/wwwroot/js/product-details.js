document.addEventListener('DOMContentLoaded', function () {
    // --- Quantity Selector Logic ---
    const decreaseBtn = document.getElementById('decrease-quantity');
    const increaseBtn = document.getElementById('increase-quantity');
    const quantityInput = document.getElementById('quantity');

    if (decreaseBtn && increaseBtn && quantityInput) {
        decreaseBtn.addEventListener('click', () => {
            let currentValue = parseInt(quantityInput.value);
            if (currentValue > 1) {
                quantityInput.value = currentValue - 1;
            }
        });

        increaseBtn.addEventListener('click', () => {
            let currentValue = parseInt(quantityInput.value);
            quantityInput.value = currentValue + 1;
        });
    }

    // --- Review Stars & Form Submission Logic ---
    const reviewForm = document.getElementById('review-form');
    if (reviewForm) {
        const stars = reviewForm.querySelectorAll('.star-rating .fa-star');
        const ratingInput = reviewForm.querySelector('#rating-value');
        const statusMessageDiv = reviewForm.querySelector('#review-status-message');

        stars.forEach(star => {
            star.addEventListener('mouseover', function () {
                resetStars();
                highlightStars(this.dataset.value);
            });

            star.addEventListener('mouseout', function () {
                resetStars();
                const currentRating = ratingInput.value;
                if (currentRating > 0) {
                    highlightStars(currentRating);
                }
            });

            star.addEventListener('click', function () {
                const rating = this.dataset.value;
                ratingInput.value = rating;
                highlightStars(rating);
            });
        });

        function highlightStars(rating) {
            for (let i = 0; i < rating; i++) {
                stars[i].classList.add('text-warning');
                stars[i].classList.remove('text-muted', 'far');
                stars[i].classList.add('fas');
            }
        }

        function resetStars() {
            stars.forEach(s => {
                s.classList.remove('text-warning');
                s.classList.add('text-muted', 'far');
                s.classList.remove('fas');
            });
        }

        reviewForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const formData = new FormData(reviewForm);
            const token = formData.get('__RequestVerificationToken');

            if (parseInt(formData.get('rating')) === 0) {
                statusMessageDiv.innerHTML = `<div class="alert alert-danger">${document.documentElement.lang === 'ar' ? 'يرجى تحديد تقييم' : 'Please select a rating'}</div>`;
                return;
            }

            try {
                const response = await fetch('/api/Reviews/Submit', {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token },
                    body: formData
                });
                const result = await response.json();
                if (response.ok && result.success) {
                    statusMessageDiv.innerHTML = `<div class="alert alert-success">${result.message}</div>`;
                    reviewForm.style.display = 'none';
                } else {
                    const errorMessage = result.message || (document.documentElement.lang === 'ar' ? 'حدث خطأ ما' : 'An error occurred.');
                    statusMessageDiv.innerHTML = `<div class="alert alert-danger">${errorMessage}</div>`;
                }
            } catch (error) {
                console.error("Review submission error:", error);
                statusMessageDiv.innerHTML = `<div class="alert alert-danger">${document.documentElement.lang === 'ar' ? 'فشل الاتصال بالخادم' : 'Failed to connect to the server.'}</div>`;
            }
        });
    }

    // --- Add to Cart Logic for Details Page ---
    const addToCartBtn = document.querySelector('.product-info-panel .btn-cart');
    if (addToCartBtn) {
        addToCartBtn.addEventListener('click', function () {
            const productCard = document.querySelector('.product-gallery');
            const productId = productCard.dataset.productId;
            const productNameEn = productCard.dataset.productNameEn;
            const productNameAr = productCard.dataset.productNameAr;
            const price = parseFloat(productCard.dataset.price);
            const image = productCard.dataset.image;
            const quantity = parseInt(document.getElementById('quantity').value);

            let cart = JSON.parse(localStorage.getItem('cart')) || [];
            let existingItem = cart.find(item => item.id === productId);

            if (existingItem) {
                existingItem.quantity += quantity;
            } else {
                cart.push({
                    id: productId,
                    nameEn: productNameEn,
                    nameAr: productNameAr,
                    price: price,
                    image: image,
                    quantity: quantity
                });
            }

            localStorage.setItem('cart', JSON.stringify(cart));

            const originalBtnContent = addToCartBtn.innerHTML;
            const lang = document.documentElement.dir === 'rtl' ? 'ar' : 'en';
            const addingText = lang === 'ar' ? 'جار الإضافة...' : 'Adding...';
            const addedText = lang === 'ar' ? 'تمت الإضافة!' : 'Added!';

            addToCartBtn.disabled = true;
            addToCartBtn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> ${addingText}`;

            setTimeout(() => {
                addToCartBtn.innerHTML = `<i class="fas fa-check me-2"></i> ${addedText}`;

                if (typeof updateCartCount === "function") {
                    updateCartCount();
                }

                const productName = lang === 'ar' ? productNameAr : productNameEn;
                const toastMessage = lang === 'ar'
                    ? `تمت إضافة ${quantity} من "${productName}" إلى السلة.`
                    : `Added ${quantity} of "${productName}" to your cart.`;

                if (typeof showToast === "function") {
                    showToast(toastMessage);
                }

                setTimeout(() => {
                    addToCartBtn.disabled = false;
                    addToCartBtn.innerHTML = originalBtnContent;
                }, 1500);

            }, 500);
        });
    }

    // --- Favorite Toggle Logic ---
    const favoriteBtn = document.querySelector('.product-info-panel .btn-favorite');
    const productGallery = document.querySelector('.product-gallery');

    if (favoriteBtn && productGallery) {
        const productId = productGallery.dataset.productId;

        favoriteBtn.addEventListener('click', async function () {
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenInput) {
                if (typeof showToast === "function") {
                    const lang = document.documentElement.dir === 'rtl' ? 'ar' : 'en';
                    const loginMessage = lang === 'ar' ? 'يرجى تسجيل الدخول أولاً' : 'Please log in first';
                    showToast(loginMessage);
                }
                return;
            }

            const token = tokenInput.value;

            try {
                const response = await fetch(`/api/Wishlist/ToggleFavorite/${productId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/json'
                    }
                });

                if (response.status === 401) {
                    if (typeof showToast === "function") {
                        const lang = document.documentElement.dir === 'rtl' ? 'ar' : 'en';
                        const loginMessage = lang === 'ar' ? 'يرجى تسجيل الدخول أولاً' : 'Please log in first';
                        showToast(loginMessage);
                    }
                    return;
                }

                if (response.ok) {
                    const result = await response.json();
                    const lang = document.documentElement.dir === 'rtl' ? 'ar' : 'en';
                    let toastMessage = '';

                    if (result.status === 'added') {
                        this.classList.add('active');
                        toastMessage = lang === 'ar' ? 'تمت الإضافة للمفضلة' : 'Added to favorites';
                    } else {
                        this.classList.remove('active');
                        toastMessage = lang === 'ar' ? 'تمت الإزالة من المفضلة' : 'Removed from favorites';
                    }

                    if (typeof showToast === "function") {
                        showToast(toastMessage);
                    }
                    if (typeof updateFavCount === "function") {
                        updateFavCount();
                    }

                } else {
                    console.error('Failed to toggle favorite status');
                    if (typeof showToast === "function") {
                        const lang = document.documentElement.dir === 'rtl' ? 'ar' : 'en';
                        const errorMessage = lang === 'ar' ? 'حدث خطأ ما' : 'Something went wrong';
                        showToast(errorMessage);
                    }
                }
            } catch (error) {
                console.error('Error toggling favorite:', error);
            }
        });
    }
});
