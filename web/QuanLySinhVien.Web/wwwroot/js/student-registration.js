document.querySelectorAll('[data-registration-form]').forEach(form => {
    form.addEventListener('submit', event => {
        // site.js opens confirmation first. Only lock after the accepted, valid submission.
        if (event.defaultPrevented) return;
        const button = form.querySelector('button[type="submit"]');
        if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
        form.dataset.submitting = 'true';
        if (button) { button.disabled = true; button.textContent = 'Đang đăng ký…'; }
        form.setAttribute('aria-busy', 'true');
    });
});
window.addEventListener('pageshow', event => {
    if (!event.persisted) return;
    document.querySelectorAll('[data-registration-form]').forEach(form => {
        delete form.dataset.submitting;
        form.removeAttribute('aria-busy');
        const button = form.querySelector('button[type="submit"]');
        if (button) { button.disabled = false; button.textContent = 'Đăng ký'; }
    });
});
