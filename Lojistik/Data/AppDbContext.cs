using Lojistik.Models;
using Microsoft.EntityFrameworkCore;

namespace Lojistik.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Firma> Firmalar => Set<Firma>();
        public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
        public DbSet<Arac> Araclar => Set<Arac>();
        public DbSet<AracBelgesi> AracBelgeleri => Set<AracBelgesi>();
        public DbSet<AracKademe> AracKademeler => Set<AracKademe>(); // [YENİ]
        public DbSet<Ulke> Ulkeler => Set<Ulke>();
        public DbSet<Sehir> Sehirler => Set<Sehir>();
        public DbSet<Musteri> Musteriler => Set<Musteri>();
        public DbSet<Lojistik.Models.Siparis> Siparisler { get; set; } = default!;
        public DbSet<Sevkiyat> Sevkiyatlar { get; set; } = default!;
        public DbSet<Sefer> Seferler { get; set; } = default!;
        public DbSet<SeferSevkiyat> SeferSevkiyatlar { get; set; } = default!;
        public DbSet<SeferMasraf> SeferMasraflari { get; set; } = default!;
        public DbSet<SeferGelir> SeferGelirleri { get; set; } = default!;
        public DbSet<Lojistik.Models.CariHareket> CariHareketler { get; set; } = default!;
        public DbSet<KademeFirma> KademeFirmalari { get; set; } = default!;

        public DbSet<ProgramAltSube>   ProgramAltSubeler   { get; set; } = null!;
        public DbSet<ProgramModul>     ProgramModulleri    { get; set; } = null!;
        public DbSet<ProgramFirmaModul> ProgramFirmaModulleri { get; set; } = null!;

        public DbSet<Sofor> Soforler { get; set; } = default!;
        public DbSet<ForwardingIs> ForwardingIsler { get; set; } = default!;
        public DbSet<ForwardingFiyat> ForwardingFiyatlar { get; set; } = default!;
        public DbSet<ForwardingKalem> ForwardingKalemler { get; set; } = default!;
        public DbSet<GuncellemeNotu> GuncellemeNotlari { get; set; } = default!;
        public DbSet<MasrafTipi> MasrafTipleri { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Modül ilişkileri ─────────────────────────────────────────────
            modelBuilder.Entity<ProgramFirmaModul>(e =>
            {
                e.HasIndex(x => new { x.FirmaID, x.ModulID })
                    .IsUnique()
                    .HasDatabaseName("UX_ProgramFirmaModulleri_Firma_Modul");

                e.HasOne(x => x.Firma)
                    .WithMany()
                    .HasForeignKey(x => x.FirmaID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ProgramFirmaModulleri_Firmalar");

                e.HasOne(x => x.Modul)
                    .WithMany()
                    .HasForeignKey(x => x.ModulID)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ProgramFirmaModulleri_Moduller");
            });

            modelBuilder.Entity<Ulke>().ToTable("Ulkeler");
            modelBuilder.Entity<Sehir>().ToTable("Sehirler");
            modelBuilder.Entity<Musteri>().ToTable("Musteriler");

           


            modelBuilder.Entity<Sehir>()
              .HasOne(s => s.Ulke)
              .WithMany(u => u.Sehirler!)
              .HasForeignKey(s => s.UlkeID);

            modelBuilder.Entity<Musteri>()
              .HasOne(m => m.Ulke)
              .WithMany()
              .HasForeignKey(m => m.UlkeID);

            modelBuilder.Entity<Musteri>()
              .HasOne(m => m.Sehir)
              .WithMany()
              .HasForeignKey(m => m.SehirID);

            modelBuilder.Entity<Firma>()
    .ToTable(tb => tb.HasTrigger("trg_Firmalar_SetUpdatedAt"));

            modelBuilder.Entity<Firma>()
                .HasIndex(x => x.FirmaKodu)
                .IsUnique();

            modelBuilder.Entity<Kullanici>()
                .HasIndex(x => x.Username)
                .IsUnique();

            // Kullanicilar tablosunda trigger var; EF Core OUTPUT clause ile çakışıyor
            modelBuilder.Entity<Kullanici>()
                .ToTable(t => t.UseSqlOutputClause(false));

            modelBuilder.Entity<Kullanici>()
                .HasOne(x => x.Firma)
                .WithMany(f => f.Kullanicilar)
                .HasForeignKey(x => x.FirmaID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AracKademe>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("getdate()")     // DB defaultu
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<AracKademe>()
                .HasOne(k => k.Arac)
                .WithMany(a => a.Kademeler)
                .HasForeignKey(k => k.AracID);

            modelBuilder.Entity<Arac>(b =>
            {
                b.HasKey(x => x.AracID);
                b.HasIndex(x => x.Plaka).IsUnique();
                b.Property(x => x.Plaka).HasMaxLength(20).IsRequired();
                b.Property(x => x.Marka).HasMaxLength(50);
                b.Property(x => x.Model).HasMaxLength(50);
                b.Property(x => x.AracTipi).HasMaxLength(30);
                b.Property(x => x.Durum).HasMaxLength(20);

                // 🔗 Firma FK
                b.Property(x => x.FirmaID).IsRequired();
                b.HasOne(x => x.Firma)
                 .WithMany()                      // Firma tarafında Araclar koleksiyonu şart değil
                 .HasForeignKey(x => x.FirmaID)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(a => a.CreatedByKullanici)
                .WithMany()
                .HasForeignKey(a => a.CreatedByKullaniciID)
                .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AracBelgesi>(b =>
            {
                b.HasKey(x => x.BelgeID);
                b.Property(x => x.BelgeTipi).HasMaxLength(30).IsRequired();
                b.Property(x => x.BelgeNo).HasMaxLength(50);
                b.Property(x => x.Firma).HasMaxLength(100);
                b.Property(x => x.ParaBirimi).HasMaxLength(10);
                b.Property(x => x.DosyaYolu).HasMaxLength(400);
                b.Property(x => x.Tutar).HasColumnType("decimal(12,2)");

                b.HasOne(x => x.Arac)
                 .WithMany(a => a.Belgeler)
                 .HasForeignKey(x => x.AracID)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => new { x.AracID, x.BelgeTipi, x.BaslangicTarihi });
                b.HasIndex(x => x.BitisTarihi);

                // Aktif belge tekilliği: aynı (AracID, BelgeTipi) için BitisTarihi NULL iken tek kayıt
                b.HasIndex(x => new { x.AracID, x.BelgeTipi })
                 .IsUnique()
                 .HasFilter("[BitisTarihi] IS NULL");
            });
            modelBuilder.Entity<ProgramAltSube>(e =>
            {
                e.ToTable("ProgramAltSubeler");
                e.HasKey(x => x.AltSubeID);
                e.Property(x => x.AltSubeKodu).HasMaxLength(20);
                e.Property(x => x.AltSubeAdi).HasMaxLength(200);
            });
            modelBuilder.Entity<Sofor>(e =>
            {
                e.ToTable("Soforler");

                e.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                e.HasIndex(p => new { p.FirmaID, p.AdSoyad }).HasDatabaseName("IX_Soforler_Firma_AdSoyad");
                e.HasIndex(p => new { p.FirmaID, p.Durum }).HasDatabaseName("IX_Soforler_Firma_Durum");

                e.HasIndex(p => new { p.FirmaID, p.TCKimlikNo })
                    .HasDatabaseName("UX_Soforler_Firma_TCKN")
                    .IsUnique()
                    .HasFilter("[TCKimlikNo] IS NOT NULL");

                e.HasIndex(p => new { p.FirmaID, p.SurucuKartNo })
                    .HasDatabaseName("UX_Soforler_Firma_SurucuKartNo")
                    .IsUnique()
                    .HasFilter("[SurucuKartNo] IS NOT NULL");

                e.HasIndex(p => new { p.FirmaID, p.PasaportNo })
                    .HasDatabaseName("UX_Soforler_Firma_Pasaport")
                    .IsUnique()
                    .HasFilter("[PasaportNo] IS NOT NULL");

                e.ToTable(t => t.HasCheckConstraint("CK_Soforler_Durum", "[Durum] IN (0,1)"));
            });
            modelBuilder.Entity<ForwardingIs>(e =>
            {
                e.HasOne(x => x.Musteri).WithMany().HasForeignKey(x => x.MusteriID).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
                e.HasOne(x => x.YukuVeren).WithMany().HasForeignKey(x => x.YukuVerenID).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
                e.HasOne(x => x.YukuAlan).WithMany().HasForeignKey(x => x.YukuAlanID).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
                e.HasOne(x => x.TasiyiciMusteri).WithMany().HasForeignKey(x => x.TasiyiciMusteriID).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
                e.HasOne(x => x.CreatedByKullanici).WithMany().HasForeignKey(x => x.CreatedByKullaniciID).OnDelete(DeleteBehavior.SetNull);
                e.HasMany(x => x.Fiyatlar).WithOne(f => f.ForwardingIs).HasForeignKey(f => f.ForwardingID).OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.BrutKg).HasColumnType("decimal(10,2)");
                e.Property(x => x.CbmHacim).HasColumnType("decimal(10,3)");
            });

            modelBuilder.Entity<ForwardingFiyat>(e =>
            {
                e.Property(x => x.Tutar).HasColumnType("decimal(18,2)");
                e.Property(x => x.FiyatTuru).HasMaxLength(10).IsRequired();
                e.Property(x => x.ParaBirimi).HasMaxLength(10).IsRequired();
            });

            modelBuilder.Entity<ForwardingKalem>(e =>
            {
                e.Property(x => x.Adet).HasColumnType("decimal(10,3)");
                e.Property(x => x.Nevi).HasMaxLength(50).IsRequired();
                e.HasOne(x => x.ForwardingIs).WithMany(f => f.Kalemler)
                 .HasForeignKey(x => x.ForwardingID).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CariHareket>(e =>
            {
                e.ToTable("CariHareketler");
                e.HasKey(x => x.CariHareketID);

                e.Property(x => x.IslemTuru).HasMaxLength(30).IsRequired();
                e.Property(x => x.EvrakNo).HasMaxLength(50);
                e.Property(x => x.Aciklama).HasMaxLength(300);
                e.Property(x => x.ParaBirimi).HasMaxLength(10).IsRequired();
                e.Property(x => x.SeferGelirID);
                e.Property(x => x.IsArsiv).HasDefaultValue(false);
                e.Property(x => x.DevirKapanmaTarihi).HasColumnType("date");
                e.Property(x => x.DevirNo).IsRequired(false);


                e.Property(x => x.Tutar).HasColumnType("decimal(18,2)");
                e.Property(x => x.Kur).HasColumnType("decimal(18,6)");

                // İndeksler
                e.HasIndex(x => new { x.FirmaID, x.MusteriID, x.Tarih })
                 .HasDatabaseName("IX_CariHareketler_Firma_Musteri_Tarih");

                e.HasIndex(x => x.IlgiliSiparisID).HasDatabaseName("IX_CariHareketler_IlgiliSiparis");
                e.HasIndex(x => x.IlgiliSevkiyatID).HasDatabaseName("IX_CariHareketler_IlgiliSevkiyat");

                // Sipariş başına tek hareket (filtered unique)
                e.HasIndex(x => new { x.FirmaID, x.IlgiliSiparisID })
                 .HasDatabaseName("UX_CariHareketler_Firma_Siparis")
                 .IsUnique()
                 .HasFilter("[IlgiliSiparisID] IS NOT NULL");
                // SeferGelir ilişkisi
                e.HasOne(x => x.SeferGelir)
                 .WithMany()
                 .HasForeignKey(x => x.SeferGelirID)
                 .OnDelete(DeleteBehavior.Restrict);

                // SeferGelir başına tek hareket (filtered unique)
                e.HasIndex(x => new { x.FirmaID, x.SeferGelirID })
                 .HasDatabaseName("UX_CariHareketler_Firma_SeferGelir")
                 .IsUnique()
                 .HasFilter("[SeferGelirID] IS NOT NULL");

            });
            modelBuilder.Entity<Siparis>(e =>
            {
                e.Property(x => x.IsCariles).HasDefaultValue(false);
                e.Property(x => x.CariEvrakNo).HasMaxLength(60);

                // FK: CariIslenenMusteriID -> Musteriler
                e.HasOne(x => x.CariIslenenMusteri)
                 .WithMany()
                 .HasForeignKey(x => x.CariIslenenMusteriID)
                 .OnDelete(DeleteBehavior.NoAction);

                // Indeksler
                e.HasIndex(x => x.IsCariles).HasDatabaseName("IX_Siparisler_IsCariles");
                e.HasIndex(x => x.CariIslenenMusteriID).HasDatabaseName("IX_Siparisler_CariIslenenMusteriID");
            });

        }
    }
}
