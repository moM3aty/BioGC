// Dictionary for holding translations for dynamic JS text
const translations = {
    en: {
        loading: 'Loading...',
        failedToLoad: 'Failed to load categories.',
        noParentCategories: 'No parent categories found.',
        noSubCategories: 'No sub-categories found for this parent.',
        selectParentFirst: 'Please select a parent category first.',
        error: 'Error!',
        success: 'Success!',
        deleted: 'Deleted!',
        oops: 'Oops...',
        areYouSure: 'Are you sure?',
        revertWarning: "You won't be able to revert this!",
        confirmDelete: 'Yes, delete it!',
        cancel: 'Cancel',
        tokenNotFound: 'Security token not found. Please refresh the page.',
        savedSuccess: 'Saved successfully',
        networkError: 'Network Error',
        networkErrorText: 'Could not connect to the server.',
        categoryDeleted: 'The category has been deleted.',
        failedToDelete: 'Failed to delete category.'
    },
    ar: {
        loading: 'جاري التحميل...',
        failedToLoad: 'فشل تحميل الفئات.',
        noParentCategories: 'لا يوجد فئات رئيسية.',
        noSubCategories: 'لا يوجد فئات فرعية لهذه الفئة الرئيسية.',
        selectParentFirst: 'الرجاء اختيار فئة رئيسية أولاً.',
        error: 'خطأ!',
        success: 'نجاح!',
        deleted: 'تم الحذف!',
        oops: 'عفوًا...',
        areYouSure: 'هل أنت متأكد؟',
        revertWarning: 'لن تتمكن من التراجع عن هذا الإجراء!',
        confirmDelete: 'نعم، قم بالحذف!',
        cancel: 'إلغاء',
        tokenNotFound: 'رمز الأمان غير موجود. يرجى تحديث الصفحة.',
        savedSuccess: 'تم الحفظ بنجاح',
        networkError: 'خطأ في الشبكة',
        networkErrorText: 'لا يمكن الاتصال بالخادم.',
        categoryDeleted: 'تم حذف الفئة بنجاح.',
        failedToDelete: 'فشل حذف الفئة.'
    }
};

const getCurrentLang = () => document.documentElement.lang || 'en';

window.addEventListener('DOMContentLoaded', event => {
    const sidebarToggle = document.body.querySelector('#sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', event => {
            event.preventDefault();
            document.body.classList.toggle('sb-sidenav-toggled');
            localStorage.setItem('sb|sidebar-toggle', document.body.classList.contains('sb-sidenav-toggled'));
        });
    }

    const currentPath = window.location.pathname.toLowerCase();
    const sidebarLinks = document.querySelectorAll('#sidebar-wrapper .list-group-item');
    let bestMatch = null;
    sidebarLinks.forEach(link => {
        try {
            const linkPath = new URL(link.href).pathname.toLowerCase();
            if (currentPath.startsWith(linkPath) && (!bestMatch || linkPath.length > new URL(bestMatch.href).pathname.length)) {
                bestMatch = link;
            }
        } catch (e) { /* Ignore invalid links */ }
    });
    if (bestMatch) {
        sidebarLinks.forEach(l => l.classList.remove('active'));
        bestMatch.classList.add('active');
    }

    if (document.getElementById('notification-list-container')) {
        setupNotifications();
    }

    initializeParentCategoryModal();
    initializeSubCategoryModal();
});

function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// --- NOTIFICATIONS ---
function setupNotifications() {
    if (typeof signalR === 'undefined') {
        console.error("SignalR client library not found.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveNotification", (messageEn, messageAr, url) => {
        const message = getCurrentLang() === 'ar' ? messageAr : messageEn;
        Swal.fire({ toast: true, position: 'top-end', showConfirmButton: false, timer: 5000, timerProgressBar: true, icon: 'info', title: message });
        fetchUnreadNotifications();
    });

    connection.onclose(error => {
        console.error(`SignalR connection closed. Retrying in 5 seconds.`);
        setTimeout(startConnection, 5000);
    });

    async function startConnection() {
        try {
            await connection.start();
            console.log("SignalR: Connected.");
            fetchUnreadNotifications();
        } catch (err) {
            console.error("SignalR: Connection failed. Retrying in 5 seconds.", err);
            setTimeout(startConnection, 5000);
        }
    }

    startConnection();

    const notificationListContainer = document.getElementById('notification-list-container');
    if (notificationListContainer) {
        // --- FIX: This is the final logic to handle clicking on a single notification ---
        notificationListContainer.addEventListener('click', function (e) {
            const notificationLink = e.target.closest('.notification-item');
            if (notificationLink) {
                e.preventDefault();

                const listItem = notificationLink.closest('li[data-notification-id]');
                const notificationId = listItem.dataset.notificationId;
                const destinationUrl = notificationLink.href;

                // This is the corrected URL that matches the controller's route template
                const fetchUrl = `/Admin/Notifications/MarkAsRead/${notificationId}`;

                fetch(fetchUrl, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
                })
                    .then(response => {
                        if (response.ok) {
                            return response.json();
                        }
                        // If the server response is not OK, we should still navigate.
                        throw new Error('Server response was not OK.');
                    })
                    .then(data => {
                        if (data.success) {
                            // Visually remove the notification and update the count before navigating
                            listItem.style.transition = 'opacity 0.3s ease';
                            listItem.style.opacity = '0';
                            setTimeout(() => {
                                listItem.remove();
                                const badge = document.getElementById('notification-badge');
                                const currentCount = parseInt(badge.textContent, 10);
                                const newCount = Math.max(0, currentCount - 1);
                                badge.textContent = newCount;
                                if (newCount === 0) {
                                    badge.style.display = 'none';
                                }
                                window.location.href = destinationUrl;
                            }, 300);
                        } else {
                            // If success is false, navigate anyway
                            window.location.href = destinationUrl;
                        }
                    })
                    .catch(error => {
                        console.error('Error marking notification as read:', error);
                        // On any error, just navigate to the destination.
                        window.location.href = destinationUrl;
                    });
            }
        });

        document.getElementById('mark-all-as-read')?.addEventListener('click', async (e) => {
            e.preventDefault();
            try {
                await fetch('/Admin/Notifications/MarkAllAsRead', { method: 'POST', headers: { 'RequestVerificationToken': getAntiForgeryToken() } });
                fetchUnreadNotifications();
            } catch (e) { console.error("Mark as read error:", e); }
        });
    }
}

// The rest of the file remains the same...
// (The functions below are unchanged)

async function fetchUnreadNotifications() {
    try {
        const response = await fetch('/Admin/Notifications/GetUnread');
        if (!response.ok) {
            console.error(`Failed to fetch notifications. Status: ${response.status}`);
            return;
        }
        const data = await response.json();
        updateNotificationUI(data);
    } catch (e) {
        console.error("Fetch notifications error:", e);
    }
}

function updateNotificationUI(data) {
    const listContainer = document.getElementById('notification-list-container');
    if (!listContainer) return;

    listContainer.querySelectorAll('li[data-notification-id]').forEach(el => el.remove());

    const noNotifMsgElement = document.getElementById('no-notifications-message');
    if (noNotifMsgElement) noNotifMsgElement.remove();

    const badge = document.getElementById('notification-badge');
    const lang = getCurrentLang();
    const firstDivider = listContainer.querySelector('.dropdown-divider');

    if (data.notifications && data.notifications.length > 0) {
        data.notifications.forEach(n => {
            const message = lang === 'ar' ? n.messageAr : n.messageEn;
            const item = document.createElement('li');
            item.dataset.notificationId = n.id;
            item.innerHTML = `<a class="dropdown-item notification-item" href="${n.url}"><div class="fw-bold">${message}</div><div class="small text-muted">${n.timestamp}</div></a>`;
            if (firstDivider) {
                firstDivider.after(item);
            } else {
                listContainer.appendChild(item);
            }
        });
    } else {
        const noNotifMsg = lang === 'ar' ? "لا توجد إشعارات جديدة" : "No new notifications";
        const li = document.createElement('li');
        li.id = 'no-notifications-message';
        li.innerHTML = `<span class="dropdown-item text-muted disabled">${noNotifMsg}</span>`;
        if (firstDivider) firstDivider.after(li);
    }

    badge.textContent = data.unreadCount;
    badge.style.display = data.unreadCount > 0 ? 'inline-block' : 'none';
}


function initializeParentCategoryModal() {
    const modalEl = document.getElementById('parentCategoryManagementModal');
    if (!modalEl) return;
    const form = document.getElementById('parent-category-modal-form');
    const idInput = document.getElementById('parentCategoryId');
    const nameEnInput = document.getElementById('parentCategoryNameEn');
    const nameArInput = document.getElementById('parentCategoryNameAr');
    const cancelBtn = document.getElementById('cancel-parent-edit-btn');
    const formTitle = document.querySelector('#parent-category-form-title span');
    modalEl.addEventListener('show.bs.modal', () => {
        resetParentCategoryForm();
        loadParentCategoriesIntoModal();
    });
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (!form.checkValidity()) { form.classList.add('was-validated'); return; }
        const id = idInput.value;
        const url = id ? `/Admin/Categories/EditParentJson` : `/Admin/Categories/CreateParentJson`;
        const method = id ? 'PUT' : 'POST';
        const token = getAntiForgeryToken();
        const lang = getCurrentLang();
        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ id: id ? parseInt(id) : 0, nameEn: nameEnInput.value, nameAr: nameArInput.value })
            });
            if (response.ok) {
                resetParentCategoryForm();
                await loadParentCategoriesIntoModal();
                await refreshAllCategoryDropdowns();
                Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: translations[lang].savedSuccess, showConfirmButton: false, timer: 2000 });
            } else {
                let errorMsg = translations[lang].oops;
                try { const errorData = await response.json(); errorMsg = errorData.message || `Server responded with status: ${response.status}`; } catch (e) { /* Ignore */ }
                Swal.fire(translations[lang].oops, errorMsg, 'error');
            }
        } catch (error) {
            Swal.fire(translations[lang].networkError, translations[lang].networkErrorText, 'error');
        }
    });
    cancelBtn.addEventListener('click', resetParentCategoryForm);
    function resetParentCategoryForm() {
        form.reset();
        idInput.value = '';
        formTitle.setAttribute('data-en', 'Add New Parent Category');
        formTitle.setAttribute('data-ar', 'إضافة فئة رئيسية جديدة');
        cancelBtn.style.display = 'none';
        if (typeof updateAdminUIForLanguage === 'function') updateAdminUIForLanguage();
    }
}
async function loadParentCategoriesIntoModal() {
    const list = document.getElementById('parent-category-list-modal');
    const lang = getCurrentLang();
    list.innerHTML = `<li class="list-group-item text-muted">${translations[lang].loading}</li>`;
    try {
        const response = await fetch('/Admin/Categories/GetParentCategoriesJson');
        const categories = await response.json();
        list.innerHTML = '';
        if (categories.length === 0) {
            list.innerHTML = `<li class="list-group-item text-muted">${translations[lang].noParentCategories}</li>`;
        } else {
            categories.forEach(cat => {
                const li = document.createElement('li');
                li.className = 'list-group-item d-flex justify-content-between align-items-center';
                li.innerHTML = `<span>${cat.name}</span><div class="btn-group btn-group-sm"><button class="btn btn-outline-primary" onclick="editParentCategory(${cat.id})" title="Edit"><i class="fas fa-pencil-alt"></i></button><button class="btn btn-outline-danger" onclick="deleteParentCategory(${cat.id})" title="Delete"><i class="fas fa-trash-alt"></i></button></div>`;
                list.appendChild(li);
            });
        }
    } catch (error) {
        list.innerHTML = `<li class="list-group-item text-danger">${translations[lang].failedToLoad}</li>`;
    }
}
async function editParentCategory(id) {
    try {
        const response = await fetch(`/Admin/Categories/GetCategoryJson/${id}`);
        if (!response.ok) throw new Error('Category not found');
        const cat = await response.json();
        const formTitle = document.querySelector('#parent-category-form-title span');
        formTitle.setAttribute('data-en', 'Edit Parent Category');
        formTitle.setAttribute('data-ar', 'تعديل الفئة الرئيسية');
        document.getElementById('parentCategoryId').value = cat.id;
        document.getElementById('parentCategoryNameEn').value = cat.nameEn;
        document.getElementById('parentCategoryNameAr').value = cat.nameAr;
        document.getElementById('cancel-parent-edit-btn').style.display = 'block';
        if (typeof updateAdminUIForLanguage === 'function') updateAdminUIForLanguage();
    } catch (error) {
        Swal.fire('Error!', 'Could not fetch category details.', 'error');
    }
}
async function deleteParentCategory(id) {
    const lang = getCurrentLang();
    Swal.fire({
        title: translations[lang].areYouSure, text: translations[lang].revertWarning, icon: 'warning',
        showCancelButton: true, confirmButtonColor: '#d33', cancelButtonColor: '#3085d6',
        confirmButtonText: translations[lang].confirmDelete, cancelButtonText: translations[lang].cancel
    }).then(async (result) => {
        if (result.isConfirmed) {
            try {
                const response = await fetch(`/Admin/Categories/DeleteParentJson/${id}`, { method: 'DELETE', headers: { 'RequestVerificationToken': getAntiForgeryToken() } });
                if (response.ok) {
                    await loadParentCategoriesIntoModal();
                    await refreshAllCategoryDropdowns();
                    Swal.fire(translations[lang].deleted, translations[lang].categoryDeleted, 'success');
                } else {
                    const errorData = await response.json();
                    Swal.fire(translations[lang].error, errorData.message || translations[lang].failedToDelete, 'error');
                }
            } catch (error) {
                Swal.fire(translations[lang].networkError, translations[lang].networkErrorText, 'error');
            }
        }
    });
}

function initializeSubCategoryModal() {
    const modalEl = document.getElementById('subCategoryManagementModal');
    if (!modalEl) return;
    const parentSelect = document.getElementById('modalParentCategorySelect');
    const subCategoryList = document.getElementById('sub-category-list-modal');
    const form = document.getElementById('sub-category-modal-form');
    const idInput = document.getElementById('subCategoryId');
    const parentIdInput = document.getElementById('subCategoryParentId');
    const nameEnInput = document.getElementById('subCategoryNameEn');
    const nameArInput = document.getElementById('subCategoryNameAr');
    const cancelBtn = document.getElementById('cancel-sub-edit-btn');
    const formTitle = document.querySelector('#sub-category-form-title span');
    modalEl.addEventListener('show.bs.modal', () => {
        resetSubCategoryForm();
        loadParentCategoriesIntoSelect(parentSelect);
    });
    parentSelect.addEventListener('change', () => {
        const parentId = parentSelect.value;
        parentIdInput.value = parentId;
        resetSubCategoryForm();
        if (parentId) {
            loadSubCategoriesIntoModal(parentId);
        } else {
            const lang = getCurrentLang();
            subCategoryList.innerHTML = `<li class="list-group-item text-muted">${translations[lang].selectParentFirst}</li>`;
        }
    });
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const lang = getCurrentLang();
        if (!parentIdInput.value) {
            Swal.fire(translations[lang].error, translations[lang].selectParentFirst, 'error');
            return;
        }
        if (!form.checkValidity()) { form.classList.add('was-validated'); return; }
        const id = idInput.value;
        const url = id ? `/Admin/Categories/EditSubCategoryJson` : `/Admin/Categories/CreateSubCategoryJson`;
        const method = id ? 'PUT' : 'POST';
        const token = getAntiForgeryToken();
        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({
                    id: id ? parseInt(id) : 0,
                    nameEn: nameEnInput.value,
                    nameAr: nameArInput.value,
                    parentCategoryId: parseInt(parentIdInput.value)
                })
            });
            if (response.ok) {
                resetSubCategoryForm();
                await loadSubCategoriesIntoModal(parentIdInput.value);
                await refreshAllSubCategoryDropdowns();
                Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: translations[lang].savedSuccess, showConfirmButton: false, timer: 2000 });
            } else {
                const errorData = await response.json();
                Swal.fire(translations[lang].error, errorData.message || 'An error occurred.', 'error');
            }
        } catch (error) {
            Swal.fire(translations[lang].networkError, translations[lang].networkErrorText, 'error');
        }
    });
    cancelBtn.addEventListener('click', resetSubCategoryForm);
    function resetSubCategoryForm() {
        form.reset();
        idInput.value = '';
        formTitle.setAttribute('data-en', 'Add New Sub-Category');
        formTitle.setAttribute('data-ar', 'إضافة فئة فرعية جديدة');
        cancelBtn.style.display = 'none';
        if (typeof updateAdminUIForLanguage === 'function') updateAdminUIForLanguage();
    }
}
async function loadParentCategoriesIntoSelect(selectElement) {
    const lang = getCurrentLang();
    selectElement.innerHTML = `<option value="">${lang === 'ar' ? 'اختر...' : 'Select...'}</option>`;
    try {
        const response = await fetch('/Admin/Categories/GetParentCategoriesJson');
        const categories = await response.json();
        categories.forEach(cat => {
            const names = cat.name.split(' / ');
            const nameToShow = lang === 'ar' ? names[1].trim() : names[0].trim();
            selectElement.add(new Option(nameToShow, cat.id));
        });
    } catch (error) {
        console.error("Failed to load parent categories for select:", error);
    }
}
async function loadSubCategoriesIntoModal(parentId) {
    const list = document.getElementById('sub-category-list-modal');
    const lang = getCurrentLang();
    list.innerHTML = `<li class="list-group-item text-muted">${translations[lang].loading}</li>`;
    try {
        const response = await fetch(`/Admin/Categories/GetSubCategoriesJson?parentId=${parentId}`);
        const categories = await response.json();
        list.innerHTML = '';
        if (categories.length === 0) {
            list.innerHTML = `<li class="list-group-item text-muted">${translations[lang].noSubCategories}</li>`;
        } else {
            categories.forEach(cat => {
                const li = document.createElement('li');
                li.className = 'list-group-item d-flex justify-content-between align-items-center';
                li.innerHTML = `<span>${cat.name}</span><div class="btn-group btn-group-sm"><button class="btn btn-outline-primary" onclick="editSubCategory(${cat.id})" title="Edit"><i class="fas fa-pencil-alt"></i></button><button class="btn btn-outline-danger" onclick="deleteSubCategory(${cat.id})" title="Delete"><i class="fas fa-trash-alt"></i></button></div>`;
                list.appendChild(li);
            });
        }
    } catch (error) {
        list.innerHTML = `<li class="list-group-item text-danger">${translations[lang].failedToLoad}</li>`;
    }
}
async function editSubCategory(id) {
    try {
        const response = await fetch(`/Admin/Categories/GetCategoryJson/${id}`);
        if (!response.ok) throw new Error('Category not found');
        const cat = await response.json();
        const formTitle = document.querySelector('#sub-category-form-title span');
        formTitle.setAttribute('data-en', 'Edit Sub-Category');
        formTitle.setAttribute('data-ar', 'تعديل الفئة الفرعية');
        document.getElementById('subCategoryId').value = cat.id;
        document.getElementById('subCategoryParentId').value = cat.parentCategoryId;
        document.getElementById('subCategoryNameEn').value = cat.nameEn;
        document.getElementById('subCategoryNameAr').value = cat.nameAr;
        document.getElementById('cancel-sub-edit-btn').style.display = 'block';
        if (typeof updateAdminUIForLanguage === 'function') updateAdminUIForLanguage();
    } catch (error) {
        Swal.fire('Error!', 'Could not fetch sub-category details.', 'error');
    }
}
async function deleteSubCategory(id) {
    const lang = getCurrentLang();
    Swal.fire({
        title: translations[lang].areYouSure, text: translations[lang].revertWarning, icon: 'warning',
        showCancelButton: true, confirmButtonColor: '#d33', cancelButtonColor: '#3085d6',
        confirmButtonText: translations[lang].confirmDelete, cancelButtonText: translations[lang].cancel
    }).then(async (result) => {
        if (result.isConfirmed) {
            const parentId = document.getElementById('modalParentCategorySelect').value;
            try {
                const response = await fetch(`/Admin/Categories/DeleteSubCategoryJson/${id}`, { method: 'DELETE', headers: { 'RequestVerificationToken': getAntiForgeryToken() } });
                if (response.ok) {
                    await loadSubCategoriesIntoModal(parentId);
                    await refreshAllSubCategoryDropdowns();
                    Swal.fire(translations[lang].deleted, translations[lang].categoryDeleted, 'success');
                } else {
                    const errorData = await response.json();
                    Swal.fire(translations[lang].error, errorData.message || translations[lang].failedToDelete, 'error');
                }
            } catch (error) {
                Swal.fire(translations[lang].networkError, translations[lang].networkErrorText, 'error');
            }
        }
    });
}

async function refreshAllCategoryDropdowns() {
    await refreshAllParentCategoryDropdowns();
    await refreshAllSubCategoryDropdowns();
}

async function refreshAllParentCategoryDropdowns() {
    // This function can be implemented if needed for other pages
}

async function refreshAllSubCategoryDropdowns() {
    const lang = getCurrentLang();
    try {
        const response = await fetch('/Admin/Products/GetSubCategorySelectListJson');
        if (!response.ok) return;
        const subCategories = await response.json();

        document.querySelectorAll('select[name="CategoryId"]').forEach(select => {
            const currentVal = select.value;
            select.innerHTML = `<option value="">-- Select Category --</option>`;
            subCategories.forEach(cat => {
                select.add(new Option(cat.text, cat.value));
            });
            select.value = currentVal;
        });
    } catch (error) {
        console.error("Failed to refresh sub-category dropdowns:", error);
    }
}
