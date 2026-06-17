/* ═══════════════════════════════════════════════════════════════
   form-helpers.js — Lojistik form yardımcıları
   • Zorunlu alan göstergesi (kırmızı *)
   • Para birimi (tutar) maskesi — TR formatı (1.250,50)
   • Telefon maskesi — esnek (sayı + boşluk + parantez + tire + +)
   ═══════════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    // ── 1) ZORUNLU ALAN GÖSTERGESİ ───────────────────────────────
    // ASP.NET Core tag helper'ları required alanlara
    // `data-val-required="..."` attribute'ı koyar. Bu attribute'a sahip
    // her input/select/textarea için ilişkili <label>'a kırmızı * ekle.
    function markRequiredLabels(root) {
        const inputs = (root || document).querySelectorAll(
            'input[data-val-required], select[data-val-required], textarea[data-val-required]'
        );
        inputs.forEach(function (el) {
            // form-check (checkbox) için label-after-input — atla
            if (el.type === 'checkbox' || el.type === 'radio' || el.type === 'hidden') return;

            const id = el.id;
            if (!id) return;

            const label = document.querySelector('label[for="' + CSS.escape(id) + '"]');
            if (!label) return;
            if (label.querySelector('.required-mark')) return; // tekrar koyma

            const mark = document.createElement('span');
            mark.className = 'required-mark';
            mark.textContent = ' *';
            mark.setAttribute('aria-hidden', 'true');
            label.appendChild(mark);
        });
    }

    // ── 2) PARA BİRİMİ MASKESİ ───────────────────────────────────
    // Hedef alanlar: name sonu Tutar, Fiyat, Maliyet, Borc, Alacak,
    // Kur, YakitLitre olan inputlar VEYA `.js-mask-tutar` sınıfı.
    const CURRENCY_NAME_PATTERN = /(Tutar|Fiyat|Maliyet|Borc|Alacak|Kur|YakitLitre)$/;

    function isCurrencyInput(el) {
        if (el.classList.contains('js-mask-tutar')) return true;
        if (el.classList.contains('js-no-mask')) return false;
        const name = el.getAttribute('name') || '';
        return CURRENCY_NAME_PATTERN.test(name);
    }

    function initCurrencyMasks(root) {
        if (typeof Cleave === 'undefined') return;
        const inputs = (root || document).querySelectorAll('input:not([data-mask-init])');
        inputs.forEach(function (el) {
            if (el.type === 'hidden' || el.type === 'checkbox' || el.type === 'radio') return;
            if (!isCurrencyInput(el)) return;

            // type="number" Cleave ile çakışır — text'e çevir
            if (el.type === 'number') el.type = 'text';

            // Mevcut değeri TR formatına çevir (eğer invariant gelmişse)
            // tr-TR culture sayesinde tag helper genelde "1.250,50" üretir;
            // ama yine de güvence için noktayı-virgülü düzeltelim.
            const raw = el.value;
            if (raw && /^\d+\.\d+$/.test(raw)) {
                // "1250.50" → "1250,50"
                el.value = raw.replace('.', ',');
            }

            new Cleave(el, {
                numeral: true,
                numeralThousandsGroupStyle: 'thousand',
                numeralDecimalMark: ',',
                delimiter: '.',
                numeralDecimalScale: 2,
                numeralPositiveOnly: false
            });

            el.setAttribute('data-mask-init', '1');
            el.setAttribute('inputmode', 'decimal');
        });
    }

    // ── 3) TELEFON MASKESİ (esnek) ───────────────────────────────
    // Yalnızca rakam, boşluk, parantez, tire ve + işaretine izin ver.
    const PHONE_NAME_PATTERN = /(Telefon|Tel|Gsm|Mobil)$/i;

    function isPhoneInput(el) {
        if (el.classList.contains('js-mask-tel')) return true;
        if (el.classList.contains('js-no-mask')) return false;
        const name = el.getAttribute('name') || '';
        return PHONE_NAME_PATTERN.test(name);
    }

    function sanitizePhone(value) {
        if (!value) return '';
        // İzin verilenler: 0-9, boşluk, +, (, ), -
        return value.replace(/[^\d+()\-\s]/g, '');
    }

    function initPhoneMasks(root) {
        const inputs = (root || document).querySelectorAll('input:not([data-mask-init])');
        inputs.forEach(function (el) {
            if (el.type === 'hidden' || el.type === 'checkbox' || el.type === 'radio') return;
            if (!isPhoneInput(el)) return;

            if (el.type === 'number') el.type = 'tel';
            else if (!el.type || el.type === 'text') el.type = 'tel';

            el.setAttribute('inputmode', 'tel');
            el.setAttribute('placeholder', el.getAttribute('placeholder') || '+90 (5XX) XXX XX XX');

            el.addEventListener('input', function () {
                const cleaned = sanitizePhone(el.value);
                if (cleaned !== el.value) {
                    const pos = el.selectionStart;
                    el.value = cleaned;
                    // imleci yaklaşık eski konuma geri koy
                    try { el.setSelectionRange(pos - 1, pos - 1); } catch (e) {}
                }
            });

            el.setAttribute('data-mask-init', '1');
        });
    }

    // ── BAŞLAT ───────────────────────────────────────────────────
    function initAll(root) {
        markRequiredLabels(root);
        initCurrencyMasks(root);
        initPhoneMasks(root);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { initAll(); });
    } else {
        initAll();
    }

    // Dışarıdan dinamik form eklenirse manuel çağrı için global erişim
    window.LjForms = { init: initAll };
})();
