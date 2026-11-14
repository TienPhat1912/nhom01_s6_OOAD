(() => {
    'use strict';
    const dialog = document.querySelector('#confirm-dialog');
    let pendingForm = null;
    let pendingButton = null;
    let confirmedForm = null;
    const setBusy = (form, button) => {
        if (form.dataset.busy) return false;
        form.dataset.busy = 'true';
        form.setAttribute('aria-busy', 'true');
        if (button) { button.dataset.originalHtml = button.innerHTML; button.disabled = true; button.innerHTML = '<span class="spinner" aria-hidden="true"></span> Đang xử lý…'; }
        document.querySelector('.page-loading').hidden = false;
        return true;
    };
    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (!form.checkValidity()) { event.preventDefault(); form.reportValidity(); return; }
        if (form.dataset.confirm && confirmedForm !== form) {
            event.preventDefault(); pendingForm = form; pendingButton = event.submitter;
            dialog.querySelector('#confirm-description').textContent = form.dataset.confirm;
            dialog.showModal(); return;
        }
        confirmedForm = null;
        if (form.hasAttribute('data-preview-form')) { event.preventDefault(); return; }
        if (!setBusy(form, event.submitter)) event.preventDefault();
    });
    dialog?.querySelector('[data-confirm-cancel]').addEventListener('click', () => dialog.close());
    dialog?.querySelector('[data-confirm-accept]').addEventListener('click', () => {
        const form = pendingForm; const button = pendingButton;
        dialog.close(); confirmedForm = form; form?.requestSubmit(button ?? undefined);
    });
    dialog?.addEventListener('close', () => { pendingButton?.focus(); pendingForm = null; pendingButton = null; });
    document.querySelectorAll('[data-toggle-password]').forEach(button => button.addEventListener('click', () => {
        const input = document.getElementById(button.dataset.togglePassword);
        const visible = input.type === 'password'; input.type = visible ? 'text' : 'password';
        button.setAttribute('aria-label', visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
        button.setAttribute('aria-pressed', String(visible));
    }));
    const toggle = document.querySelector('[data-nav-toggle]');
    const sidebar = document.querySelector('#sidebar');
    const scrim = document.querySelector('[data-nav-close]');
    const workspace = document.querySelector('.workspace');
    const mobile = window.matchMedia('(max-width: 900px)');
    const setNav = (open, restoreFocus = true) => {
        document.body.classList.toggle('nav-open', open);
        toggle?.setAttribute('aria-expanded', String(open));
        toggle?.setAttribute('aria-label', open ? 'Đóng menu' : 'Mở menu');
        if (workspace) workspace.inert = open;
        if (scrim) scrim.hidden = !open;
        if (open) sidebar?.querySelector('a')?.focus(); else if (restoreFocus) toggle?.focus();
    };
    toggle?.addEventListener('click', () => setNav(!document.body.classList.contains('nav-open')));
    scrim?.addEventListener('click', () => setNav(false));
    document.querySelector('[data-sidebar-close]')?.addEventListener('click', () => setNav(false));
    mobile.addEventListener('change', () => setNav(false, false));
    document.addEventListener('keydown', event => {
        if (!document.body.classList.contains('nav-open')) return;
        if (event.key === 'Escape') setNav(false);
        if (event.key === 'Tab') {
            const links = sidebar.querySelectorAll('a, button');
            const first = links[0], last = links[links.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });
    window.addEventListener('pageshow', () => {
        setNav(false, false);
        document.querySelectorAll('form[data-busy]').forEach(form => { delete form.dataset.busy; form.removeAttribute('aria-busy'); });
        document.querySelectorAll('button[data-original-html]').forEach(button => { button.innerHTML = button.dataset.originalHtml; delete button.dataset.originalHtml; button.disabled = false; });
        document.querySelector('.page-loading').hidden = true;
    });
})();

document.querySelectorAll("[data-print]").forEach(button => button.addEventListener("click", () => window.print()));
