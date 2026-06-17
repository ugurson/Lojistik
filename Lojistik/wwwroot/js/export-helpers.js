/* ═══════════════════════════════════════════════════════════════
   export-helpers.js — İndir dropdown'u için Excel/PDF/Yazdır
   • Excel:  SheetJS ile gerçek .xlsx (görünen tabloyu alır)
   • PDF:    window.print() — kullanıcı diyalogdan "PDF olarak kaydet"
   • Yazdır: window.print() (print-only CSS uygulanır)
   ═══════════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    function tsStamp() {
        const d = new Date();
        const pad = n => String(n).padStart(2, '0');
        return d.getFullYear()
             + pad(d.getMonth() + 1)
             + pad(d.getDate()) + '_'
             + pad(d.getHours())
             + pad(d.getMinutes());
    }

    function findTable(selector) {
        // Önce tam selector, sonra ilk .lj-table fallback
        let t = selector ? document.querySelector(selector) : null;
        if (!t) t = document.querySelector('.lj-table');
        return t;
    }

    // ── EXCEL: SheetJS ile XLSX üret ─────────────────────────────
    function exportExcel(button) {
        if (typeof XLSX === 'undefined') {
            alert('Excel dışa aktarma kütüphanesi yüklenemedi. Sayfayı yenileyip tekrar deneyin.');
            return;
        }
        const sel  = button.getAttribute('data-table');
        const base = button.getAttribute('data-file') || 'liste';
        const table = findTable(sel);
        if (!table) {
            alert('Dışa aktarılacak tablo bulunamadı.');
            return;
        }

        // Tablo klonu üzerinden çalış — "İşlemler" sütununu ve gizli satırları çıkar
        const clone = table.cloneNode(true);

        // Son sütun (İşlemler) — th/td index'ini hesapla ve tüm satırlardan çıkar
        const headerCells = clone.querySelectorAll('thead th');
        let actionIdx = -1;
        headerCells.forEach((th, i) => {
            const txt = (th.textContent || '').trim().toLowerCase();
            if (txt === 'i̇şlemler' || txt === 'işlemler') actionIdx = i;
        });
        if (actionIdx >= 0) {
            clone.querySelectorAll('tr').forEach(tr => {
                const cells = tr.children;
                if (cells[actionIdx]) cells[actionIdx].remove();
            });
        }

        // Boş satırlar (örn. "Kayıt yok") da kalsın ama "rowlink" sınıfı problem değil
        const wb = XLSX.utils.table_to_book(clone, { sheet: base.substring(0, 28), raw: false });
        const fname = base + '_' + tsStamp() + '.xlsx';
        XLSX.writeFile(wb, fname);
    }

    // ── PDF / YAZDIR: window.print() ─────────────────────────────
    function exportPrint(opts) {
        // Print başlamadan önce sayfaya geçici başlık ekle (rapor görünümü)
        // İsteyen sayfa zaten kendi başlığını gösteriyor (h2)
        window.print();
    }

    // ── EVENT DELEGATION ─────────────────────────────────────────
    document.addEventListener('click', function (e) {
        const t = e.target.closest('.lj-export-excel, .lj-export-pdf, .lj-export-print');
        if (!t) return;

        e.preventDefault();
        if (t.classList.contains('lj-export-excel'))      exportExcel(t);
        else if (t.classList.contains('lj-export-pdf'))   exportPrint({ asPdf: true });
        else if (t.classList.contains('lj-export-print')) exportPrint({ asPdf: false });
    });

    window.LjExport = { excel: exportExcel, print: exportPrint };
})();
