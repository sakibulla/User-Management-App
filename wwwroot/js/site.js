/*
 * IMPORTANT: This file is included globally via _Layout.cshtml.
 * It sets up the antiforgery token header for all fetch() calls that
 * need to POST to ASP.NET Core endpoints with [ValidateAntiForgeryToken].
 *
 * NOTE: ASP.NET Core accepts the token either as a form field OR as the
 * "RequestVerificationToken" HTTP header — we use the header for JSON requests.
 */

/**
 * getUniqIdValue — retrieves the ASP.NET Core antiforgery (XSRF) token value
 * from the hidden input field rendered by @Html.AntiForgeryToken().
 *
 * IMPORTANT: Must be called once the DOM is loaded. Returns an empty string
 * if the token input is not present (e.g. on public pages).
 *
 * @returns {string} The antiforgery token string
 */
function getUniqIdValue() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    // NOTA BENE: Returns empty string rather than throwing — callers must handle
    return input ? input.value : '';
}

/**
 * showToast — displays a Bootstrap 5 toast notification.
 * NOTE: Uses Bootstrap's Toast JS component (included via bootstrap.bundle).
 * No browser alerts are used per assignment requirement.
 *
 * @param {string} message - Text to display
 * @param {'success'|'error'} type - Controls color/icon
 */
function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const id = 'toast-' + Date.now();
    const icon    = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';
    const bgClass = type === 'success' ? 'bg-success' : 'bg-danger';

    const html = `
      <div id="${id}" class="toast align-items-center text-white ${bgClass} border-0"
           role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
          <div class="toast-body">
            <i class="bi ${icon} me-2"></i>${message}
          </div>
          <button type="button" class="btn-close btn-close-white me-2 m-auto"
                  data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
      </div>`;

    container.insertAdjacentHTML('beforeend', html);
    const el = document.getElementById(id);
    const toast = new bootstrap.Toast(el, { delay: 3500 });
    toast.show();
    // NOTE: Remove from DOM after hidden to avoid memory leak
    el.addEventListener('hidden.bs.toast', () => el.remove());
}
