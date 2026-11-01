(() => {
    const rows = document.getElementById('curriculum-rows');
    const template = document.getElementById('course-row-template');
    const add = document.getElementById('add-course');
    if (!rows || !template || !add) return;
    const reindex = () => {
        [...rows.children].forEach((row, index) => {
            row.querySelectorAll('input,select').forEach(field => {
                const property = field.dataset.field || field.name.split('.').pop();
                field.name = `MonHocs[${index}].${property}`;
            });
        });
        add.disabled = rows.children.length >= 200;
    };
    add.addEventListener('click', () => {
        if (rows.children.length >= 200) return;
        rows.append(template.content.cloneNode(true)); reindex();
        rows.lastElementChild.querySelector('select').focus();
    });
    rows.addEventListener('click', event => {
        const button = event.target.closest('[data-remove-course]');
        if (!button) return;
        button.closest('tr').remove(); reindex();
    });
    document.getElementById('curriculum-form').addEventListener('submit', event => {
        if (event.submitter?.hasAttribute('formnovalidate')) return;
        if (rows.children.length === 0) { event.preventDefault(); add.click(); }
    });
    if (!rows.children.length) add.click();
    reindex();
})();
