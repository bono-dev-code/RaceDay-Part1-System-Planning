document.querySelectorAll("form[data-confirm]").forEach(form => {
    form.addEventListener("submit", event => {
        if (!window.confirm(form.dataset.confirm || "Are you sure?")) event.preventDefault();
    });
});
