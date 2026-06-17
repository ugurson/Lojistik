/* ═══════════════════════════════════════════════════════════════
   page-progress.js — Sayfalar arası geçişlerde üst-bant ilerleme
   NProgress.js wrapper'ı: link tıklama ve form submit'lerinde başlatır
   ═══════════════════════════════════════════════════════════════ */
(function () {
    'use strict';
    if (typeof NProgress === 'undefined') return;

    NProgress.configure({
        showSpinner: false,
        trickleSpeed: 180,
        minimum: 0.12,
        easing: 'ease',
        speed: 350
    });

    // Sayfa yüklenirken başlat → load'da bitir
    NProgress.start();
    window.addEventListener('load', function () { NProgress.done(); });
    window.addEventListener('pageshow', function (ev) {
        // back/forward cache geri dönüşünde de bitir
        if (ev.persisted) NProgress.done();
    });

    // Aynı origin'e giden link tıklamalarında başlat
    function isInternalNav(a) {
        if (!a || !a.href) return false;
        if (a.target && a.target !== '' && a.target !== '_self') return false;
        if (a.hasAttribute('download')) return false;
        if (a.dataset.noProgress !== undefined) return false;

        const href = a.getAttribute('href') || '';
        if (!href || href.startsWith('#')) return false;
        if (href.startsWith('javascript:')) return false;
        if (href.startsWith('mailto:') || href.startsWith('tel:')) return false;

        try {
            const url = new URL(a.href, window.location.href);
            if (url.origin !== window.location.origin) return false;
            // Aynı sayfa + sadece hash değişiyorsa atla
            if (url.pathname === window.location.pathname &&
                url.search   === window.location.search &&
                url.hash) return false;
            return true;
        } catch (e) {
            return false;
        }
    }

    document.addEventListener('click', function (e) {
        if (e.defaultPrevented) return;
        if (e.button !== 0) return;                   // sadece sol tık
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;  // yeni sekme vb. atla

        const a = e.target.closest('a');
        if (!a) return;
        if (!isInternalNav(a)) return;

        NProgress.start();
    });

    // Form submit'lerde başlat (GET filtreler + POST kayıt)
    document.addEventListener('submit', function (e) {
        const f = e.target;
        if (!f || f.tagName !== 'FORM') return;
        if (f.dataset.noProgress !== undefined) return;
        NProgress.start();
    });
})();
