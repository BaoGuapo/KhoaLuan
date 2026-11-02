const initToast = (toast) => {
    const closeButton = toast.querySelector('.toast-close');
    let timeoutId;

    const close = () => {
        window.clearTimeout(timeoutId);
        toast.classList.add('hide');
        toast.addEventListener('transitionend', () => toast.remove(), { once: true });
        window.setTimeout(() => toast.remove(), 400);
    };

    closeButton?.addEventListener('click', close);
    window.requestAnimationFrame(() => toast.classList.add('show'));
    timeoutId = window.setTimeout(close, 5000);
};
document.querySelectorAll('[data-toast]').forEach(initToast);

// Cho các trang dùng JavaScript hiện thông báo cùng kiểu với thông báo từ server.
window.showToast = (title, text, kind = 'success') => {
    const region = document.querySelector('.toast-region');
    if (!region) return;
    const toast = document.createElement('div');
    toast.className = `toast toast-${kind}`;
    toast.setAttribute('role', kind === 'success' ? 'status' : 'alert');
    const icon = document.createElement('span');
    icon.className = 'toast-icon'; icon.setAttribute('aria-hidden', 'true'); icon.textContent = kind === 'success' ? '✓' : '!';
    const body = document.createElement('div');
    const strong = document.createElement('strong'); strong.textContent = title;
    const p = document.createElement('p'); p.textContent = text;
    body.append(strong, p);
    const closeButton = document.createElement('button');
    closeButton.type = 'button'; closeButton.className = 'toast-close'; closeButton.setAttribute('aria-label', 'Đóng thông báo'); closeButton.textContent = '×';
    const progress = document.createElement('span');
    progress.className = 'toast-progress'; progress.setAttribute('aria-hidden', 'true');
    toast.append(icon, body, closeButton, progress);
    region.append(toast);
    initToast(toast);
};

const navigationToggle = document.querySelector('[data-nav-toggle]');
const navigation = document.querySelector('#primary-nav');
navigationToggle?.addEventListener('click', () => {
    const isOpen = navigationToggle.getAttribute('aria-expanded') === 'true';
    navigationToggle.setAttribute('aria-expanded', String(!isOpen));
    navigationToggle.setAttribute('aria-label', isOpen ? 'Mở menu điều hướng' : 'Đóng menu điều hướng');
    navigation?.classList.toggle('open', !isOpen);
});

const confirmationDialog = document.querySelector('[data-confirm-dialog]');
const confirmationMessage = confirmationDialog?.querySelector('[data-confirm-message]');
const acceptConfirmation = confirmationDialog?.querySelector('[data-confirm-accept]');
const cancelConfirmation = confirmationDialog?.querySelector('[data-confirm-cancel]');
let pendingConfirmation;

document.querySelectorAll('form[data-confirm]').forEach((form) => {
    form.addEventListener('submit', (event) => {
        if (form.dataset.confirmed === 'true') {
            delete form.dataset.confirmed;
            return;
        }
        event.preventDefault();
        if (!confirmationDialog?.showModal) {
            if (window.confirm(form.dataset.confirm)) {
                form.dataset.confirmed = 'true';
                form.requestSubmit(event.submitter || undefined);
            }
            return;
        }
        pendingConfirmation = { form, submitter: event.submitter };
        confirmationMessage.textContent = form.dataset.confirm;
        confirmationDialog.showModal();
    });
});

cancelConfirmation?.addEventListener('click', () => confirmationDialog.close());
confirmationDialog?.addEventListener('close', () => { pendingConfirmation = undefined; });
acceptConfirmation?.addEventListener('click', () => {
    const pending = pendingConfirmation;
    confirmationDialog.close();
    if (!pending) return;
    pending.form.dataset.confirmed = 'true';
    pending.form.requestSubmit(pending.submitter || undefined);
});
