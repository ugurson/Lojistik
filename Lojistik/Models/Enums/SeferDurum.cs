namespace Lojistik.Models.Enums
{
    public enum SeferDurum : byte
    {
        Yeni       = 0,
        Planlandi  = 1,
        Kapali     = 2,
        Tamamlandi = 3,
        Iptal      = 4
    }

    public static class SeferDurumExtensions
    {
        // SeferDurum enum'u üzerinde tanımlı — SiparisDurumExtensions ile çakışmaz
        public static string ToAd(this SeferDurum durum) => durum switch
        {
            SeferDurum.Yeni       => "Yeni",
            SeferDurum.Planlandi  => "Planlandı",
            SeferDurum.Kapali     => "Kapalı",
            SeferDurum.Tamamlandi => "Tamamlandı",
            SeferDurum.Iptal      => "İptal",
            _                     => $"Durum {(byte)durum}"
        };

        public static string ToBadgeClass(this SeferDurum durum) => durum switch
        {
            SeferDurum.Yeni       => "bg-secondary",
            SeferDurum.Planlandi  => "bg-info text-dark",
            SeferDurum.Kapali     => "bg-dark",
            SeferDurum.Tamamlandi => "bg-success",
            SeferDurum.Iptal      => "bg-danger",
            _                     => "bg-light text-dark"
        };
    }
}
