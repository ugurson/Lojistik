-- =====================================================
-- Fix_CariViews_Yonu.sql
-- vw_CariBakiye ve vw_CariEkstre yon/isaret eslemesini kanonik
-- konvansiyona hizalar.
--
-- KANONIK YON (teyit edildi):
--   Yonu = 1 -> BORC  (musteri bize borclu, bakiyeyi ARTIRIR -> +Tutar)
--   Yonu = 0 -> ALACAK (tahsilat/iskonto, musteri lehine, bakiyeyi AZALTIR -> -Tutar)
--
-- Tum yazma kodu, DB filtered index'i ve gercek veri bu yondedir.
-- Bu iki view ESKIDEN ters idi (Yonu=0'i Borc, Yonu=1'i Alacak sayiyordu)
-- ve "0=Borc, 1=Alacak" yorumu yanlisti. Bu script SADECE Yonu isaret
-- eslemesini ve yorumlari duzeltir; kolonlar, GROUP BY, IsArsiv=0 filtresi
-- ve diger tum mantik AYNEN korunmustur.
--
-- Idempotent: CREATE OR ALTER kullanir, tekrar calistirilabilir.
-- Bu view'lerin hicbir tuketicisi (kod / DB nesnesi / harici rapor) yoktur;
-- duzeltme mevcut hicbir davranisi bozmaz.
-- =====================================================

-- -------------------------------------------------------
-- 1) vw_CariBakiye : Yonu=1 -> +Tutar, Yonu=0 -> -Tutar
--    (musteri borclu = POZITIF bakiye)
-- -------------------------------------------------------
CREATE OR ALTER VIEW [dbo].[vw_CariBakiye]
AS
SELECT
    ch.FirmaID,
    ch.MusteriID,
    -- Doviz bazinda net bakiye  (1=Borc -> +, 0=Alacak -> -)
    SUM(CASE WHEN ch.ParaBirimi = N'TL'  THEN CASE WHEN ch.Yonu=1 THEN ch.Tutar ELSE -ch.Tutar END ELSE 0 END) AS Bakiye_TL,
    SUM(CASE WHEN ch.ParaBirimi = N'EUR' THEN CASE WHEN ch.Yonu=1 THEN ch.Tutar ELSE -ch.Tutar END ELSE 0 END) AS Bakiye_EUR,
    SUM(CASE WHEN ch.ParaBirimi = N'USD' THEN CASE WHEN ch.Yonu=1 THEN ch.Tutar ELSE -ch.Tutar END ELSE 0 END) AS Bakiye_USD,
    -- TL karsiligi (kurla carpilmis kalemler)  (1=Borc -> +, 0=Alacak -> -)
    SUM(CASE WHEN ch.Yonu=1 THEN ch.TutarTL ELSE -ch.TutarTL END) AS Bakiye_TL_Karsilik
FROM dbo.CariHareketler ch
WHERE ch.IsArsiv = 0   -- Arsivlenen (devredilmis) kayitlar bakiyeye dahil edilmez
GROUP BY ch.FirmaID, ch.MusteriID;
GO

-- -------------------------------------------------------
-- 2) vw_CariEkstre : Borc -> Yonu=1, Alacak -> Yonu=0
-- -------------------------------------------------------
CREATE OR ALTER VIEW [dbo].[vw_CariEkstre]
AS
SELECT
    ch.CariHareketID,
    ch.FirmaID,
    ch.SubeKodu,
    ch.MusteriID,
    ch.Tarih,
    ch.VadeTarihi,
    ch.IslemTuru,
    ch.EvrakNo,
    ch.Aciklama,
    ch.ParaBirimi,
    ch.Yonu,                           -- 1=Borc, 0=Alacak
    CASE WHEN ch.Yonu=1 THEN ch.Tutar ELSE 0 END AS Borc,
    CASE WHEN ch.Yonu=0 THEN ch.Tutar ELSE 0 END AS Alacak,
    ch.Kur,
    ch.TutarTL,
    ch.IlgiliSiparisID,
    ch.IlgiliSevkiyatID,
    ch.IsArsiv,
    ch.DevirKapanmaTarihi
FROM dbo.CariHareketler ch
WHERE ch.IsArsiv = 0;  -- Arsivlenen (devredilmis) kayitlar ekstreden cikarilir
GO

-- =====================================================
-- DOGRULAMA (opsiyonel, salt-okuma) -- uygulamaniz uzerinde calistirilabilir:
--
--   SELECT MusteriID, Bakiye_EUR FROM dbo.vw_CariBakiye WHERE MusteriID = 2;
--
-- Beklenen: Bakiye_EUR = +31200.00  (musteri borclu, POZITIF).
-- Duzeltmeden ONCE bu deger -31200.00 idi. Buyukluk ayni, isaret artik dogru
-- yonde -> duzeltmenin dogrulugunu kanitlar.
-- =====================================================
