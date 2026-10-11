document.querySelectorAll('[data-toast]').forEach((toast) => {
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
});

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
