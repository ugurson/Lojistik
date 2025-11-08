using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;

public interface ICurrencyRateService
{
    /// <summary>İstenen tarihte PB->TRY satış kuru döner (ör. "USD","EUR"). TL için 1 döner.</summary>
    Task<decimal> GetTryRateAsync(string currencyCode, DateTime date);
}

public class CurrencyRateService : ICurrencyRateService
{
    private readonly HttpClient _http;
    public CurrencyRateService(HttpClient http) => _http = http;

    public async Task<decimal> GetTryRateAsync(string currencyCode, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return 1m;
        var code = currencyCode.Trim().ToUpperInvariant();
        if (code == "TRY" || code == "TL") return 1m;

        // 1) TCMB: today.xml veya tarihli xml (max 3 iş günü geri dene)
        for (int i = 0; i < 3; i++)
        {
            var d = date.Date.AddDays(-i);
            var url = BuildTcmbUrl(d);
            try
            {
                var xmlStr = await _http.GetStringAsync(url);
                var xdoc = XDocument.Parse(xmlStr);
                var node = xdoc.Root?
                    .Elements("Currency")
                    .FirstOrDefault(e => (string?)e.Attribute("CurrencyCode") == code);

                if (node != null)
                {
                    // ForexSelling genelde piyasa için yeterli (Banknote satış da var)
                    var raw = (string?)node.Element("ForexSelling") ?? (string?)node.Element("BanknoteSelling");
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0)
                        return rate;
                }
            }
            catch
            {
                // yoksa bir gün geri dene
            }
        }

        // 2) Frankfurter (ECB) fallback: doğrudan USD->TRY, EUR->TRY isteği
        try
        {
            var path = date.Date == DateTime.Today ? "latest" : date.ToString("yyyy-MM-dd");
            var url = $"https://api.frankfurter.app/{path}?from={code}&to=TRY";
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty("TRY", out var tryEl) &&
                tryEl.TryGetDecimal(out var fx) && fx > 0)
            {
                return fx;
            }
        }
        catch { /* ignore */ }

        // Son çare: 1 (değiştirmek istersen burada exception da fırlatabilirsin)
        return 1m;
    }

    private static string BuildTcmbUrl(DateTime d)
    {
        // Bugün için today.xml; geçmiş için /kurlar/yyyyMM/ddMMyyyy.xml
        var today = DateTime.Today;
        if (d.Date == today)
            return "https://www.tcmb.gov.tr/kurlar/today.xml";
        var yyyymm = d.ToString("yyyyMM");
        var ddMMyyyy = d.ToString("ddMMyyyy");
        return $"https://www.tcmb.gov.tr/kurlar/{yyyymm}/{ddMMyyyy}.xml";
    }
}
