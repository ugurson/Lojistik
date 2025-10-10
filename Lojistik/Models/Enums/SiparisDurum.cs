namespace Lojistik.Models.Enums
{
    public enum SiparisDurum : byte
    {
        Acik = 0,
        Planlandi = 1,
        Yuklendi = 2,
        TeslimEdildi = 3,
        Iptal = 9
    }

    public static class SiparisDurumExtensions
    {
        public static string ToAd(this byte durum)
        {
            return durum switch
            {
                (byte)SiparisDurum.Acik => "Açık",
                (byte)SiparisDurum.Planlandi => "Planlandı",
                (byte)SiparisDurum.Yuklendi => "Yüklendi",
                (byte)SiparisDurum.TeslimEdildi => "Teslim Edildi",
                (byte)SiparisDurum.Iptal => "İptal",
                _ => $"Durum {durum}"
            };
        }

        public static string ToBadgeClass(this byte durum)
        {
            return durum switch
            {
                (byte)SiparisDurum.Acik => "bg-secondary",
                (byte)SiparisDurum.Planlandi => "bg-info",
                (byte)SiparisDurum.Yuklendi => "bg-primary",
                (byte)SiparisDurum.TeslimEdildi => "bg-success",
                (byte)SiparisDurum.Iptal => "bg-danger",
                _ => "bg-light text-dark"
            };
        }
    }
}
