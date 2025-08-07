// This script handles the language switching functionality for the admin panel.

// Get the saved language from localStorage, default to 'en' if not found.
let adminCurrentLanguage = localStorage.getItem('adminLanguage') || 'en';

// Translations object to hold direction for each language.
const adminTranslations = {
    en: { direction: 'ltr' },
    ar: { direction: 'rtl' }
};

/**
 * Updates the entire UI based on the current language.
 * This function is the core of the translation system.
 */
function updateAdminUIForLanguage() {
    const lang = adminCurrentLanguage;
    const dir = adminTranslations[lang].direction;
    const langToggleSwitch = document.getElementById('adminLangToggleSwitch');

    const html = document.documentElement;
    html.setAttribute('dir', dir);
    html.setAttribute('lang', lang);


    // Set a cookie that the server can read. Expires in 1 year.
    document.cookie = `language=${lang};path=/;max-age=31536000;SameSite=Lax`;

    // Update the state of the toggle switch.
    if (langToggleSwitch) {
        langToggleSwitch.checked = (lang === 'ar');
    }

    // Update text content for elements with data-en/data-ar attributes.
    document.querySelectorAll('[data-en], [data-ar]').forEach(el => {
        const text = el.getAttribute(`data-${lang}`);
        if (text !== null) {
            el.textContent = text;
        }
    });

    // Update placeholders for input elements.
    document.querySelectorAll('[data-en-placeholder], [data-ar-placeholder]').forEach(el => {
        const placeholder = el.getAttribute(`data-${lang}-placeholder`);
        if (placeholder !== null) {
            el.setAttribute('placeholder', placeholder);
        }
    });

    // Refresh category dropdowns if they exist to show the correct language
    if (typeof refreshAllCategoryDropdowns === 'function') {
        const selects = document.querySelectorAll('select[name="ParentCategoryId"], select[name="categoryId"]');
        if (selects.length > 0) {
            refreshAllCategoryDropdowns();
        }
    }
}

/**
 * Toggles the language between 'en' and 'ar', saves the preference,
 * and triggers the UI update.
 */
function toggleAdminLanguage() {
    adminCurrentLanguage = adminCurrentLanguage === 'en' ? 'ar' : 'en';
    localStorage.setItem('adminLanguage', adminCurrentLanguage);
    updateAdminUIForLanguage();

    // If SignalR notifications are present, re-fetch them to update language
    if (typeof fetchUnreadNotifications === 'function') {
        fetchUnreadNotifications();
    }

    
    window.location.reload();
}

// --- Event Listeners ---
document.addEventListener('DOMContentLoaded', () => {
    const langToggleSwitch = document.getElementById('adminLangToggleSwitch');
    if (langToggleSwitch) {
        langToggleSwitch.addEventListener('change', toggleAdminLanguage);
    }
    // Initial UI update on page load.
    updateAdminUIForLanguage();
});
