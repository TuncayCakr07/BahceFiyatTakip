namespace BahceFiyatTakip.Services.ExcelSeed;

public record MarketInfo(string Name, string? BaseUrl, string? SearchUrlTemplate, bool IsActive);

public static class MarketNormalizer
{
    private static readonly Dictionary<string, MarketInfo> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Portakalbahcem ──────────────────────────────────────────────────
        ["Portakalbahcem"]               = M("Portakalbahcem", "https://www.portakalbahcem.com", null, true),
        ["Portakalbahçem"]               = M("Portakalbahcem", "https://www.portakalbahcem.com", null, true),
        ["portakalbahcem.com"]           = M("Portakalbahcem", "https://www.portakalbahcem.com", null, true),
        ["portakalbahcem(kırmızı)"]      = M("Portakalbahcem", "https://www.portakalbahcem.com", null, true),
        ["Portakalbahcem 6 adet 250 Ml"] = M("Portakalbahcem", "https://www.portakalbahcem.com", null, true),

        // ── Migros ──────────────────────────────────────────────────────────
        ["Migros"]              = M("Migros", "https://www.migros.com.tr", "https://www.migros.com.tr/rest/products/search?q={0}&sayfa=0&sira=ONERILENLER&webSubdomain=www", true),
        ["Migros Sanal Market"] = M("Migros", "https://www.migros.com.tr", "https://www.migros.com.tr/rest/products/search?q={0}&sayfa=0&sira=ONERILENLER&webSubdomain=www", true),
        ["Migrossanalmarket"]   = M("Migros", "https://www.migros.com.tr", "https://www.migros.com.tr/rest/products/search?q={0}&sayfa=0&sira=ONERILENLER&webSubdomain=www", true),

        // ── Macrocenter ─────────────────────────────────────────────────────
        ["Macro"]       = M("Macrocenter", "https://www.macrocenter.com.tr", "https://www.macrocenter.com.tr/arama?q={0}", true),
        ["Macrocenter"] = M("Macrocenter", "https://www.macrocenter.com.tr", "https://www.macrocenter.com.tr/arama?q={0}", true),

        // ── CarrefourSA ──────────────────────────────────────────────────────
        ["Carrefour"]    = M("CarrefourSA", "https://www.carrefoursa.com", "https://www.carrefoursa.com/search/?text={0}", true),
        ["Carrefoursa"]  = M("CarrefourSA", "https://www.carrefoursa.com", "https://www.carrefoursa.com/search/?text={0}", true),
        ["CarrefourSA"]  = M("CarrefourSA", "https://www.carrefoursa.com", "https://www.carrefoursa.com/search/?text={0}", true),

        // ── Şok ─────────────────────────────────────────────────────────────
        ["Şok"]        = M("Şok", "https://www.sokmarket.com.tr", "https://www.sokmarket.com.tr/arama?q={0}", true),
        ["sokmarket"]  = M("Şok", "https://www.sokmarket.com.tr", "https://www.sokmarket.com.tr/arama?q={0}", true),

        // ── A101 ─────────────────────────────────────────────────────────────
        ["A101"]  = M("A101", "https://www.a101.com.tr", "https://www.a101.com.tr/arama/{0}/", true),
        ["A 101"] = M("A101", "https://www.a101.com.tr", "https://www.a101.com.tr/arama/{0}/", true),

        // ── Metro ────────────────────────────────────────────────────────────
        ["Metro"] = M("Metro", "https://metro-online.com.tr", null, false),

        // ── Anamur Bahçesi ───────────────────────────────────────────────────
        ["Anamur Bahçesi"] = M("Anamur Bahçesi", "https://www.anamurbahcesi.com", "https://www.anamurbahcesi.com/?s={0}", true),
        ["Anamurbahçesi"]  = M("Anamur Bahçesi", "https://www.anamurbahcesi.com", "https://www.anamurbahcesi.com/?s={0}", true),
        ["Anamurbahcesi"]  = M("Anamur Bahçesi", "https://www.anamurbahcesi.com", "https://www.anamurbahcesi.com/?s={0}", true),

        // ── Eski Tadında ─────────────────────────────────────────────────────
        ["Eski Tadında"]  = M("Eski Tadında", "https://www.eskitadinda.com", "https://www.eskitadinda.com/?s={0}", true),
        ["Eskitadında"]   = M("Eski Tadında", "https://www.eskitadinda.com", "https://www.eskitadinda.com/?s={0}", true),
        ["eskitadinda"]   = M("Eski Tadında", "https://www.eskitadinda.com", "https://www.eskitadinda.com/?s={0}", true),
        ["Eski tadında"]  = M("Eski Tadında", "https://www.eskitadinda.com", "https://www.eskitadinda.com/?s={0}", true),

        // ── Taze Direkt ─────────────────────────────────────────────────────
        ["Taze Direkt"] = M("Taze Direkt", "https://www.tazedirekt.com", "https://www.tazedirekt.com/arama?q={0}", true),
        ["Tazedirekt"]  = M("Taze Direkt", "https://www.tazedirekt.com", "https://www.tazedirekt.com/arama?q={0}", true),

        // ── Taze Dükkan ─────────────────────────────────────────────────────
        ["Taze Dükkan"] = M("Taze Dükkan", "https://www.tazedukkan.com.tr", "https://www.tazedukkan.com.tr/search?q={0}", true),
        ["Tazedükkan"]  = M("Taze Dükkan", "https://www.tazedukkan.com.tr", "https://www.tazedukkan.com.tr/search?q={0}", true),
        ["Taze Dukkan"] = M("Taze Dükkan", "https://www.tazedukkan.com.tr", "https://www.tazedukkan.com.tr/search?q={0}", true),

        // ── Turkuazköy ───────────────────────────────────────────────────────
        ["Turkuazköy"]  = M("Turkuazköy", "https://www.turkuazkoy.com", "https://www.turkuazkoy.com/?s={0}", true),
        ["turkuazkoy"]  = M("Turkuazköy", "https://www.turkuazkoy.com", "https://www.turkuazkoy.com/?s={0}", true),

        // ── Hediyelik Bahçem ─────────────────────────────────────────────────
        ["Hediyelik Bahçem"] = M("Hediyelik Bahçem", "https://www.hediyelikbahcem.com", "https://www.hediyelikbahcem.com/?s={0}", true),
        ["Hediyelikbahçem"]  = M("Hediyelik Bahçem", "https://www.hediyelikbahcem.com", "https://www.hediyelikbahcem.com/?s={0}", true),
        ["Hediyelikbahcem"]  = M("Hediyelik Bahçem", "https://www.hediyelikbahcem.com", "https://www.hediyelikbahcem.com/?s={0}", true),

        // ── Yeşil Limon ─────────────────────────────────────────────────────
        ["Yeşil Limon"] = M("Yeşil Limon", "https://www.yesillimon.com", "https://www.yesillimon.com/?s={0}", true),
        ["yeşillimon"]  = M("Yeşil Limon", "https://www.yesillimon.com", "https://www.yesillimon.com/?s={0}", true),
        ["Yesillimon"]  = M("Yeşil Limon", "https://www.yesillimon.com", "https://www.yesillimon.com/?s={0}", true),

        // ── Gürmar ──────────────────────────────────────────────────────────
        ["Gürmar"] = M("Gürmar", "https://www.gurmar.com.tr", "https://www.gurmar.com.tr/search?q={0}", true),
        ["Gurmar"] = M("Gürmar", "https://www.gurmar.com.tr", "https://www.gurmar.com.tr/search?q={0}", true),

        // ── Sebzemeyvedünyası ────────────────────────────────────────────────
        ["Sebze Meyve Dünyası"]         = M("Sebzemeyvedünyası", "https://www.sebzemeyvedunyasi.com", null, false),
        ["Sebzemeyvedunyası"]           = M("Sebzemeyvedünyası", "https://www.sebzemeyvedunyasi.com", null, false),
        ["Sebzemeyvedünyası"]           = M("Sebzemeyvedünyası", "https://www.sebzemeyvedunyasi.com", null, false),
        ["Sebzemeyvedünyası (Verita)"]  = M("Sebzemeyvedünyası", "https://www.sebzemeyvedunyasi.com", null, false),
        ["Sebzemeyvedünyası(HB)"]       = M("Sebzemeyvedünyası", "https://www.sebzemeyvedunyasi.com", null, false),
        ["Hepsiburada Sebzemeyvedünyası"] = M("Sebzemeyvedünyası", "https://www.sebzemeyvedunyasi.com", null, false),
        ["Verita"]                      = M("Sebzemeyvedünyası", "https://www.sebzemeyvedunyasi.com", null, false),

        // ── Avokadocu Ayşe ───────────────────────────────────────────────────
        ["Avokadocu Ayşe"] = M("Avokadocu Ayşe", null, null, false),
        ["AvokadocuAyşe"]  = M("Avokadocu Ayşe", null, null, false),
        ["Avokadocuayşe"]  = M("Avokadocu Ayşe", null, null, false),

        // ── Nuribey Çiftliği ─────────────────────────────────────────────────
        ["Nuri Bey"]          = M("Nuribey Çiftliği", null, null, false),
        ["Nuribey"]           = M("Nuribey Çiftliği", null, null, false),
        ["Nuribey Çiftliği"]  = M("Nuribey Çiftliği", null, null, false),
        ["Nuribeyçiftliği"]   = M("Nuribey Çiftliği", null, null, false),
        ["NuribeyÇiftliği"]   = M("Nuribey Çiftliği", null, null, false),

        // ── Naciye Hanımın Çiftliği ──────────────────────────────────────────
        ["Naciye Hanımın Çiftliği"] = M("Naciye Hanımın Çiftliği", null, null, false),
        ["Naciyehanımçiftliği"]     = M("Naciye Hanımın Çiftliği", null, null, false),
        ["NaciyeHanımınÇiftliği"]   = M("Naciye Hanımın Çiftliği", null, null, false),
        ["Naciye Hanım Çiftliği"]   = M("Naciye Hanımın Çiftliği", null, null, false),

        // ── Büyükannem ───────────────────────────────────────────────────────
        ["Büyükannem"] = M("Büyükannem", "https://www.buyukannem.com", null, false),
        ["Buyukannem"] = M("Büyükannem", "https://www.buyukannem.com", null, false),

        // ── Gülen Bahçe ──────────────────────────────────────────────────────
        ["Gülen Bahçe"] = M("Gülen Bahçe", null, null, false),
        ["Gülenbahçe"]  = M("Gülen Bahçe", null, null, false),
        ["Gulenbahce"]  = M("Gülen Bahçe", null, null, false),

        // ── Meyvebahçeniz ────────────────────────────────────────────────────
        ["Meyvebahçeniz"]  = M("Meyvebahçeniz", null, null, false),
        ["Meyve Bahçeniz"] = M("Meyvebahçeniz", null, null, false),

        // ── Sarıyer Market ───────────────────────────────────────────────────
        ["Sarıyer Market"] = M("Sarıyer Market", null, null, false),
        ["Sarıyermarket"]  = M("Sarıyer Market", null, null, false),
        ["Sariyermarket"]  = M("Sarıyer Market", null, null, false),

        // ── Anamurdalından ───────────────────────────────────────────────────
        ["Anamurdalından"] = M("Anamurdalından", null, null, false),
        ["Anamurdalindan"] = M("Anamurdalından", null, null, false),

        // ── Tropik Sepeti ────────────────────────────────────────────────────
        ["Tropik Sepeti"] = M("Tropik Sepeti", null, null, false),
        ["Tropiksepeti"]  = M("Tropik Sepeti", null, null, false),

        // ── Fresh Selective ──────────────────────────────────────────────────
        ["Fresh Selective"] = M("Fresh Selective", null, null, false),
        ["Freshselective"]  = M("Fresh Selective", null, null, false),

        // ── Panayır Gourmet ──────────────────────────────────────────────────
        ["Panayır Gourmet"] = M("Panayır Gourmet", null, null, false),
        ["Panayırgourmet"]  = M("Panayır Gourmet", null, null, false),
        ["Panayir Gourmet"] = M("Panayır Gourmet", null, null, false),

        // ── Turuncubağ ───────────────────────────────────────────────────────
        ["Turuncubağ"] = M("Turuncubağ", null, null, false),
        ["turuncubag"] = M("Turuncubağ", null, null, false),

        // ── Söyle Yerinden ───────────────────────────────────────────────────
        ["Söyle Yerinden"] = M("Söyle Yerinden", null, null, false),
        ["Söyleyerinden"]  = M("Söyle Yerinden", null, null, false),
        ["Soyle Yerinden"] = M("Söyle Yerinden", null, null, false),

        // ── Kaptan Tarım ─────────────────────────────────────────────────────
        ["Kaptan Tarım"] = M("Kaptan Tarım", null, null, false),
        ["Kaptan Tarim"] = M("Kaptan Tarım", null, null, false),

        // ── Lokmacıana ───────────────────────────────────────────────────────
        ["Lokmacıana"] = M("Lokmacıana", null, null, false),
        ["Lokmaciana"] = M("Lokmacıana", null, null, false),

        // ── Dünyanınmeyvesi ──────────────────────────────────────────────────
        ["Dünyanınmeyvesi"]  = M("Dünyanınmeyvesi", null, null, false),
        ["Dunyaninmeyvesi"]  = M("Dünyanınmeyvesi", null, null, false),

        // ── Babamın Bahçesi ──────────────────────────────────────────────────
        ["Babamın Bahçesi"]      = M("Babamın Bahçesi", null, null, false),
        ["Babamın Bahçesi (HB)"] = M("Babamın Bahçesi", null, null, false),
        ["Babaminbahcesi"]       = M("Babamın Bahçesi", null, null, false),

        // ── Hatayköy Yöresel ─────────────────────────────────────────────────
        ["Hatayköy Yöresel"] = M("Hatayköy Yöresel", null, null, false),
        ["Hataykoyyoresel"]  = M("Hatayköy Yöresel", null, null, false),

        // ── Damlıca Çiftliği ─────────────────────────────────────────────────
        ["Damlıca Çiftliği"] = M("Damlıca Çiftliği", null, null, false),
        ["Damlicaciftligi"]  = M("Damlıca Çiftliği", null, null, false),

        // ── DatçaMuratÇiftliği ───────────────────────────────────────────────
        ["DatçaMuratÇiftliği"]  = M("Datça Murat Çiftliği", null, null, false),
        ["Datça Murat Çiftliği"] = M("Datça Murat Çiftliği", null, null, false),

        // ── Çamlık Gıda ─────────────────────────────────────────────────────
        ["Çamlık Gıda"] = M("Çamlık Gıda", null, null, false),
        ["Camlik Gida"] = M("Çamlık Gıda", null, null, false),

        // ── Dalından Lezzet ──────────────────────────────────────────────────
        ["Dalından Lezzet"] = M("Dalından Lezzet", null, null, false),
        ["Dalindan Lezzet"] = M("Dalından Lezzet", null, null, false),

        // ── Ejder Meyvesi (site) ─────────────────────────────────────────────
        ["Ejdermeyvesi"]     = M("Ejdermeyvesi", null, null, false),
        ["Ejder Meyvesi"]    = M("Ejdermeyvesi", null, null, false),  // market/site, not product

        // ── Organikgiller ────────────────────────────────────────────────────
        ["Organikgiller"] = M("Organikgiller", null, null, false),

        // ── Enmanav ──────────────────────────────────────────────────────────
        ["Enmanav"] = M("Enmanav", null, null, false),

        // ── Avokadolu ────────────────────────────────────────────────────────
        ["Avokadolu"] = M("Avokadolu", null, null, false),

        // ── Musko ────────────────────────────────────────────────────────────
        ["Musko"] = M("Musko", null, null, false),

        // ── Nuhunambarı ──────────────────────────────────────────────────────
        ["Nuhunambarı"] = M("Nuhunambarı", null, null, false),
        ["Nuhunambarı"] = M("Nuhunambarı", null, null, false),

        // ── Organik Limon ────────────────────────────────────────────────────
        ["Organik Limon"] = M("Organik Limon", null, null, false),

        // ── Kuzey Tropik ─────────────────────────────────────────────────────
        ["Kuzey Tropik(Hepsiburada)"] = M("Kuzey Tropik", null, null, false),
        ["Kuzey Tropik"]              = M("Kuzey Tropik", null, null, false),

        // ── N11 (Doğalkasa) ──────────────────────────────────────────────────
        ["N11 (Doğalkasa)"] = M("N11 (Doğalkasa)", null, null, false),
        ["N11 Doğalkasa"]   = M("N11 (Doğalkasa)", null, null, false),

        // ── Hepsi Bahçemden ──────────────────────────────────────────────────
        ["Hepsi Bahçemden"] = M("Hepsi Bahçemden", null, null, false),

        // ── İstegelsin ───────────────────────────────────────────────────────
        ["İstegelsin"] = M("İstegelsin", null, null, false),
        ["Istegelsin"] = M("İstegelsin", null, null, false),

        // ── Köyceğiz Yöresel ─────────────────────────────────────────────────
        ["Köyceğiz Yöresel(Pazarama)"] = M("Köyceğiz Yöresel", null, null, false),
        ["Köyceğiz Yöresel"]           = M("Köyceğiz Yöresel", null, null, false),

        // ── Taze Masa ────────────────────────────────────────────────────────
        ["Taze Masa"] = M("Taze Masa", null, null, false),

        // ── Topla Sepeti ─────────────────────────────────────────────────────
        ["Topla Sepeti"] = M("Topla Sepeti", null, null, false),
        ["Toplasepeti"]  = M("Topla Sepeti", null, null, false),

        // ── Tropikal Türkiye ─────────────────────────────────────────────────
        ["Tropikal Türkiye"] = M("Tropikal Türkiye", null, null, false),
        ["Tropikalturkiye"]  = M("Tropikal Türkiye", null, null, false),

        // ── Otçu ─────────────────────────────────────────────────────────────
        ["Otçu"] = M("Otçu", null, null, false),
        ["Otcu"] = M("Otçu", null, null, false),

        // ── Arden ────────────────────────────────────────────────────────────
        ["Arden"] = M("Arden", null, null, false),

        // ── Entazem ──────────────────────────────────────────────────────────
        ["Entazem"] = M("Entazem", null, null, false),

        // ── Afta Market ──────────────────────────────────────────────────────
        ["Afta Market"] = M("Afta Market", null, null, false),
        ["Aftamarket"]  = M("Afta Market", null, null, false),

        // ── Çiftçideneve ─────────────────────────────────────────────────────
        ["Çiftçideneve"] = M("Çiftçideneve", null, null, false),
        ["Ciftcideneve"] = M("Çiftçideneve", null, null, false),

        // ── Gurmeköy ─────────────────────────────────────────────────────────
        ["Gurmeköy"] = M("Gurmeköy", null, null, false),
        ["Gurmekoy"] = M("Gurmeköy", null, null, false),

        // ── Altanaturel ──────────────────────────────────────────────────────
        ["Altanaturel"]  = M("Altanaturel", null, null, false),
        ["Altanatürel"]  = M("Altanaturel", null, null, false),

        // ── Bodrum Mandalina ─────────────────────────────────────────────────
        ["Bodrum Mandalina"]  = M("Bodrum Mandalina", null, null, false),
        ["Bodrummandalina"]   = M("Bodrum Mandalina", null, null, false),

        // ── Bim ──────────────────────────────────────────────────────────────
        ["Bim"]  = M("BİM", "https://www.bim.com.tr", null, false),
        ["BİM"]  = M("BİM", "https://www.bim.com.tr", null, false),
    };

    private static MarketInfo M(string name, string? baseUrl, string? searchTpl, bool isActive)
        => new(name, baseUrl, searchTpl, isActive);

    public static MarketInfo Normalize(string rawName)
    {
        rawName = rawName.Trim();
        if (_map.TryGetValue(rawName, out var info)) return info;

        // Auto-clean unknown market names
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            rawName,
            @"\s*\((HB|Hepsiburada|Pazarama|Verita)\)\s*$",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

        if (cleaned.Length > 79) cleaned = cleaned[..79];
        return M(cleaned, null, null, false);
    }
}
