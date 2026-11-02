// Hộp thoại sửa thủ công điểm danh trên trang lịch sử: điền sẵn sinh viên và trạng thái hiện tại.
(() => {
    const dialog = document.querySelector('[data-correct-dialog]');
    if (!dialog) return;
    const status = dialog.querySelector('[data-correct-status]');
    const reason = dialog.querySelector('textarea[name="lyDo"]');

    document.querySelectorAll('[data-correct-open]').forEach((button) => {
        button.addEventListener('click', () => {
            dialog.querySelector('[data-correct-code]').value = button.dataset.student;
            dialog.querySelector('[data-correct-student]').textContent = `${button.dataset.student} · ${button.dataset.name}`;
            // Chọn sẵn trạng thái khác trạng thái hiện tại, vì sửa thành trùng sẽ bị từ chối.
            const current = button.dataset.status;
            status.value = current === 'CoMat' ? 'Vang' : 'CoMat';
            [...status.options].forEach((option) => { option.disabled = option.value === current; });
            reason.value = '';
            dialog.showModal();
            reason.focus();
        });
    });
    dialog.querySelector('[data-correct-cancel]').addEventListener('click', () => dialog.close());
})();
