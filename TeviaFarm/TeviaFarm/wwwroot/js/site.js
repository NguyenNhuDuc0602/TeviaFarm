(() => {
  function getToastClasses(type) {
    switch ((type || '').toLowerCase()) {
      case 'danger':
      case 'error':
        return 'text-bg-danger';
      case 'warning':
        return 'text-bg-warning';
      case 'info':
        return 'text-bg-info';
      case 'success':
      default:
        return 'text-bg-success';
    }
  }

  function showToast(message, type = 'success', opts = {}) {
    const toastEl = document.getElementById('appToast');
    const bodyEl = document.getElementById('appToastBody');
    if (!toastEl || !bodyEl) return;

    bodyEl.textContent = message || '';

    toastEl.classList.remove('text-bg-success', 'text-bg-danger', 'text-bg-warning', 'text-bg-info');
    toastEl.classList.add(getToastClasses(type));

    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, {
      delay: typeof opts.delay === 'number' ? opts.delay : 3200,
      autohide: opts.autohide !== false
    });
    toast.show();
  }

  // Expose minimal API for inline usage if needed
  window.TeviaPopup = {
    toast: showToast,
    confirm: (message, onOk, options = {}) => {
      const modalEl = document.getElementById('confirmModal');
      const bodyEl = document.getElementById('confirmModalBody');
      const titleEl = document.getElementById('confirmModalTitle');
      const okBtn = document.getElementById('confirmModalOk');
      if (!modalEl || !bodyEl || !titleEl || !okBtn) return;

      titleEl.textContent = options.title || 'Xác nhận';
      bodyEl.textContent = message || 'Bạn có chắc chắn muốn thực hiện thao tác này?';
      okBtn.textContent = options.okText || 'Xác nhận';
      okBtn.classList.remove('btn-danger', 'btn-success', 'btn-primary');
      const variant = (options.okVariant || 'danger').toLowerCase();
      okBtn.classList.add(variant === 'success' ? 'btn-success' : variant === 'primary' ? 'btn-primary' : 'btn-danger');

      const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

      const handler = () => {
        okBtn.removeEventListener('click', handler);
        modal.hide();
        if (typeof onOk === 'function') onOk();
      };
      okBtn.addEventListener('click', handler);

      modal.show();
    }
  };

  // Intercept forms/buttons with data-confirm-message and show modal
  document.addEventListener('submit', (e) => {
    const form = e.target;
    if (!(form instanceof HTMLFormElement)) return;

    const msg = form.getAttribute('data-confirm-message');
    if (!msg) return;

    if (form.dataset.confirmed === 'true') {
      form.dataset.confirmed = '';
      return;
    }

    e.preventDefault();
    window.TeviaPopup.confirm(msg, () => {
      form.dataset.confirmed = 'true';
      form.requestSubmit ? form.requestSubmit() : form.submit();
    }, {
      title: form.getAttribute('data-confirm-title') || 'Xác nhận',
      okText: form.getAttribute('data-confirm-ok') || 'Xóa'
    });
  });

  // Show toast from server TempData (if present)
  document.addEventListener('DOMContentLoaded', () => {
    const el = document.getElementById('serverToastData');
    if (!el) return;
    const message = el.getAttribute('data-message') || '';
    const type = el.getAttribute('data-type') || 'success';
    if (message) showToast(message, type);
  });
})();

