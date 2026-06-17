-- Güncelleme Notları tablosu
CREATE TABLE dbo.GuncellemeNotlari
(
    GuncellemeNotuID    INT            IDENTITY(1,1) NOT NULL,
    Baslik              NVARCHAR(200)  NOT NULL,
    Aciklama            NVARCHAR(4000) NULL,
    Bolum               NVARCHAR(100)  NULL,
    Surum               NVARCHAR(20)   NULL,
    Tarih               DATETIME       NOT NULL DEFAULT GETDATE(),
    IsActive            BIT            NOT NULL DEFAULT 1,
    CreatedAt           DATETIME       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_GuncellemeNotlari PRIMARY KEY (GuncellemeNotuID)
);

-- İlk örnek kayıt (isteğe bağlı)
-- INSERT INTO dbo.GuncellemeNotlari (Baslik, Aciklama, Bolum, Surum, Tarih, IsActive)
-- VALUES (N'İlk Sürüm', N'Sistem yayına alındı.', N'Genel', N'v1.0.0', GETDATE(), 1);
