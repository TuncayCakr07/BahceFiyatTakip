using BahceFiyatTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace BahceFiyatTakip.Data;

// (varietyId, url)
// VarietyId referansı: 1=Okitsu, 2=Satsuma, 3=Klemantin, 4=Orri, 5=Murcott
//   6=Mayer, 7=KırmızıLimon, 8=Enterdonat, 9=Lamas, 10=Kokulu, 11=Grande, 12=TatlıLimon
//   13=Lime, 14=KaffirLime, 17=FingerVerde, 18=FingerRose, 19=Sunpearl, 20=Blossom
//   21=Washington, 22=Valensiya, 23=Mysticrimson, 24=Blushsweet, 25=ŞekerPortakalı
//   26=Hass, 27=Ettinger, 28=Bacon, 29=Fuerte, 30=Cliffton
//   31=Bergamot, 32=Kumkuat, 33=Şadok, 34=BeyazGreyfurt, 35=KırmızıGreyfurt, 36=Limekuat, 37=Turunç
//   38=Hicaz, 39=Hicaz9Eylül
//   40=KırmızıEjder, 41=SarıEjder
//   42=MorÇarkıfelek, 43=SarıÇarkıfelek
//   44=Kent(Mango), 45=TommyAtkins, 46=Keitt
//   47=SalkımlıDomates, 48=SanMarzano, 49=CherryDomates
//   50=TazeLimonOtu

internal static class DirectUrlSeed
{
    // Market adı → domain eşleşmeleri (seed'de nasıl kayıtlı varsa o isim)
    private static readonly Dictionary<string, string> DomainToMarketName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["migros.com.tr"]               = "Migros",
        ["carrefoursa.com"]             = "CarrefourSA",
        ["macrocenter.com.tr"]          = "Macrocenter",
        ["sokmarket.com.tr"]            = "Sok",
        ["portakalbahcem.com"]          = "Portakalbahcem",
        ["yesillimon.com"]              = "Yeşil Limon",
        ["tazedirekt.com"]              = "Taze Direkt",
        ["naciyehanimciftligi.com"]     = "Naciye Hanımın Çiftliği",
        ["anamurbahcesi.com"]           = "Anamur Bahçesi",
        ["sariyermarket.com"]           = "Sarıyer Market",
        ["gurmar.com.tr"]               = "Gürmar",
        ["hediyelikbahcem.com"]         = "Hediyelikbahçem",
        ["turkuazkoy.com"]              = "Turkuazköy",
        ["solmeraciftligi.com"]         = "Solmera Çiftliği",
        ["soyleyerinden.com"]           = "Söyle Yerinden",
        ["tazedukkan.com.tr"]           = "Taze Dükkan",
        ["avokadocuayse.com"]           = "Avokadocu Ayşe",
        ["tropiksepeti.com"]            = "Tropik sepeti",
        ["eskitadinda.com"]             = "Eski Tadında",
        ["gulenbahce.com"]              = "Gülen Bahçe",
        ["turuncubag.com"]              = "Turuncubağ",
        ["sebzemeyvedunyasi.com"]       = "Sebzemeyvedünyası",
        ["tazegel.com"]                 = "Tazegel",
        ["datcamuratciftligi.com"]      = "DatçaMuratÇiftliği",
        ["tazemasa.com"]                = "Taze Masa",
        ["nuribeyciftligi.com"]         = "Nuribeyçiftliği",
        ["buyukannem.com"]              = "Büyükannem",
        ["organikgiller.com"]           = "Organikgiller",
        ["dalindanlezzet.com"]          = "Dalından Lezzet",
        ["enmanav.com"]                 = "Enmanav",
        ["fresh.com.tr"]                = "Fresh Selective",
        ["avokadolu.com"]               = "Avokadolu",
        ["gurmekoy.com"]                = "Gurmeköy",
        ["entazem.com"]                 = "Entazem",
        ["babaminbahcesi.com"]          = "Babamın Bahçesi",
        ["egepazarindan.com"]           = "Ege Pazarından",
        ["toplasepeti.com"]             = "Topla Sepeti",
        ["istegelsin.com"]              = "İstegelsin",
        ["damlicaciftligi.com"]         = "Damlıca Çiftliği",
        ["kaptantarim.com.tr"]          = "Kaptan Tarım",
        ["meyvebahceniz.com"]           = "Meyvebahçeniz",
        ["ejdermeyvesi.com"]            = "Ejdermeyvesi",
        ["anamurdalindan.com"]          = "Anamurdalından",
        ["hataykoy.com"]                = "Hatayköy Yöresel",
        ["organiklimon.com"]            = "Organik Limon",
        ["sahidenorganik.com"]          = "Sahiden Organik",
        // İşlenmiş ürün marketleri
        ["sekerhanim.com"]              = "Şeker Hanım",
        ["antalyarecelcisi.com"]        = "Antalya Reçelcisi",
        ["bodrumyadigari.com"]          = "Bodrum Yadigarı",
        ["degirmencidede.com"]          = "Değirmencidede",
        ["yoreselyasam.com"]            = "Yöreselyaşam",
        ["dogalyasam.net"]              = "Doğalyaşam",
        ["polsgurme.com"]               = "Pol's Gourme",
        ["beyorganik.com"]              = "Beyorganik",
        ["sofyaninarkabahcesi.com"]     = "Sofya'nınarkabahçesi",
        ["arifoglu.com"]                = "Arifoğlu",
        ["marketpaketi.com.tr"]         = "Marketpaketi(Ancora)",
        ["ancora.com.tr"]               = "Ancora",
        ["nartalya.com"]                = "Nartalya 950 Ml",
        ["yorukana.com"]                = "Yörükana",
        ["pttavm.com"]                  = "PTTAVM AOÇ 200 Ml",
        ["elitenaturel.com"]            = "Elite Organic 200 ml 4'lü",
        ["gurmeninyeri.com"]            = "Gurmenin yeri",
        ["otcubitki.com"]               = "Otçu",
        ["bizeikicay.com"]              = "İkiçay",
        ["ciftcideneve.com"]            = "Çiftçideneve",
        ["muskoshop.com"]               = "Musko",
        ["kurual.com"]                  = "Kurual",
        ["ozgurleblebi.com"]            = "Özgürleblebi",
        ["egeyedonus.com"]              = "Ege'ye Dönüş",
        ["malatyapazaripalanci.com.tr"] = "Malatya Pazarı",
        ["birfincan.com"]               = "Birfincan",
        ["onadeger.com"]                = "Onadeğer",
        ["citlekci.com.tr"]             = "Çitlekçi",
        ["fxfood.com.tr"]               = "f(x) food",
        ["umitkuruyemis.com.tr"]        = "Ümit Kuruyemiş",
        ["unalkuruyemis.com.tr"]        = "Ünal Kuruyemiş",
        ["magaza.hammaddeler.com"]      = "Hammaddelergupguru",
        ["altanaturel.com"]             = "AltaNaturel",
        ["yenigunstore.com"]            = "Yenigün",
        ["tazedukkan.com.tr"]           = "Taze Dükkan",
        // Meyve suyu marketleri
        ["egepazarindan.com"]           = "Ege Pazarından",
        // Diğer yeni marketler
        ["ardenmarket.com.tr"]          = "Arden Market",
        ["dunyaninmeyvesi.com"]         = "Dünyanın Meyvesi",
        ["hepsiburada.com"]             = "Hepsiburada",
        ["mutlusebzeler.com"]           = "Mutlu Sebzeler",
        ["n11.com"]                     = "N11",
        ["nuhunambari.com"]             = "Nuhun Ambarı",
        ["olbagro.com"]                 = "OlbAgro",
        ["online.metro-tr.com"]         = "Metro",
        ["panayirgourmet.com"]          = "Panayır Gurme",
        ["pazarama.com"]                = "Pazarama",
        ["tosmagaza.com"]               = "Tos Magaza",
        ["tropikalturkiye.com"]         = "Tropikal Türkiye",
    };

    private static readonly (int VarietyId, string Url)[] Links =
    [
        // ══════════════════════════════════════════════════════
        // WASHINGTON PORTAKAL (21)
        // ══════════════════════════════════════════════════════
        (21, "https://sahidenorganik.com/product.php?slug=organik-portakal"),
        (21, "https://www.migros.com.tr/portakal-kg-p-1a0a047"),
        (21, "https://www.portakalbahcem.com/finike-portakali-yemelik-washington-1-kg-pwf005"),
        (21, "https://www.portakalbahcem.com/finike-portakali-yemelik-washington-3-kg"),
        (21, "https://www.portakalbahcem.com/ilk-hasat-finike-portakali-yemelik-washington-1-kg-pwf001"),
        (21, "https://www.macrocenter.com.tr/organik-portakal-washington-p-1a09231"),
        (21, "https://www.macrocenter.com.tr/portakal-kg-p-1a0ac49"),
        (21, "https://www.yesillimon.com/yemelik-portakal-1kg"),
        (21, "https://www.tazedirekt.com/organik-portakal-1-kg-p-1a0abf8"),
        (21, "https://www.tazedukkan.com.tr/organik-sertifikali-portakal-2-kg-urun1269.html"),
        (21, "https://www.turkuazkoy.com/finike-portakali-"),
        (21, "https://www.gurmar.com.tr/portakal-kg"),
        (21, "https://www.anamurbahcesi.com/urun/organik-yafa-portakal"),
        (21, "https://www.naciyehanimciftligi.com/valencia-portakali-1-kg"),
        (21, "https://www.babaminbahcesi.com/washington-portakal-1kg"),
        (21, "https://www.sebzemeyvedunyasi.com/portakal-finike"),
        (21, "https://turuncubag.com/urun/washington-portakal-1-kg/"),
        (21, "https://www.eskitadinda.com/finike-portakali-p"),
        (21, "https://www.sariyermarket.com/portakal-cavdir-kg"),
        (21, "https://www.enmanav.com/portakal-wasington"),

        // VALENSİYA PORTAKAL (22) - sıkmalık
        (22, "https://www.carrefoursa.com/valensiya-portakal-kg-p-30026054"),
        (22, "https://www.carrefoursa.com/portakal-valencia-kg-p-30026054"),
        (22, "https://www.carrefoursa.com/sikma-portakal-kg-p-30092100"),
        (22, "https://www.carrefoursa.com/portakal-sikmalik-kg-p-30092100"),
        (22, "https://www.migros.com.tr/portakal-sikma-file-kg-p-1a090a1"),
        (22, "https://www.macrocenter.com.tr/portakal-sikma-lux-kg-p-1a0ac3a"),
        (22, "https://www.portakalbahcem.com/yazlik-yemelik-portakal-valensiya-1-kg-pvf001"),
        (22, "https://www.portakalbahcem.com/yazlik-yemelik-portakal-valensiya-3-kg-pvf002"),
        (22, "https://www.portakalbahcem.com/yazlik-sikmalik-portakal-valensiya-3-kg-pvf004"),
        (22, "https://www.portakalbahcem.com/ilk-hasat-finike-portakali-sikmalik-washington-3-kg-pwf004"),
        (22, "https://www.tazedirekt.com/organik-sikmalik-portakal-1-kg-p-1a090a2"),
        (22, "https://www.yesillimon.com/valencia-portakal-1kg"),
        (22, "https://www.gurmar.com.tr/portakal-s%C4%B1kmal%C4%B1k-kg"),
        (22, "https://www.sariyermarket.com/portakal-sikma-kg"),
        (22, "https://www.enmanav.com/portakal-valencia-kg"),
        (22, "https://www.naciyehanimciftligi.com/valencia-portakali-1-kg"),
        (22, "https://www.datcamuratciftligi.com/urun/organik-sikmalik-portakal-kg-1"),
        (22, "https://www.gulenbahce.com/urun/valencia-yerli-portakal-sulu-dogal/"),
        (22, "https://www.sebzemeyvedunyasi.com/portakal-sikmalik"),

        // MYSTİCRİMSON KAN PORTAKAL (23)
        (23, "https://www.portakalbahcem.com/mysticrimson-kan-portakali-1-kg"),
        (23, "https://www.portakalbahcem.com/mysticrimson-kan-portakali-3-kg"),
        (23, "https://www.yesillimon.com/kan-portakali-1kg"),
        (23, "https://www.migros.com.tr/portakal-kan-file-kg-p-1a0967e"),
        (23, "https://www.eskitadinda.com/kan-portakali-p"),
        (23, "https://turuncubag.com/urun/kirmizi-washington-portakal-1-kg/"),
        (23, "https://www.nuribeyciftligi.com/urun/kan-portakal-1kg/"),

        // BLUSHSWEET KAN PORTAKAL (24)
        (24, "https://www.portakalbahcem.com/blushsweet-kan-portakali-1-kg"),
        (24, "https://www.portakalbahcem.com/blushsweet-kan-portakali-3-kg"),

        // ŞEKER PORTAKALI (25)
        (25, "https://www.portakalbahcem.com/seker-portakali-3-kg-psf002"),
        (25, "https://www.yesillimon.com/seker-portakali-1kg"),
        (25, "https://www.turuncubag.com/seker-portakali-4kg"),

        // ══════════════════════════════════════════════════════
        // LİMON ÇEŞİTLERİ
        // ══════════════════════════════════════════════════════

        // MAYER LİMON (6)
        (6, "https://www.portakalbahcem.com/mayer-limon-1-kg-lmf003"),
        (6, "https://www.portakalbahcem.com/mayer-limon-3-kg-lmf004"),
        (6, "https://www.portakalbahcem.com/ilk-hasat-mayer-limon-1-kg-lmf001"),
        (6, "https://www.portakalbahcem.com/ilk-hasat-mayer-limon-3-kg-lmf002"),
        (6, "https://www.carrefoursa.com/limon-mayer-kg-p-30006959"),
        (6, "https://www.tazedirekt.com/organik-limon-500-g-p-1a05dd9"),
        (6, "https://www.avokadocuayse.com/urunlerimiz/mayer-limon-1-kg/"),
        (6, "https://www.anamurdalindan.com/mayer-limon-1kg-"),

        // KIRMIZI LİMON (7)
        (7, "https://www.portakalbahcem.com/kirmizi-limon-1-kg-lkf004"),
        (7, "https://www.portakalbahcem.com/kirmizi-limon-3-kg-lkf005"),
        (7, "https://www.macrocenter.com.tr/kirmizi-limon-kg-p-1a00400"),

        // ENTERDONAT LİMON (8)
        (8, "https://www.portakalbahcem.com/enterdonat-limon-3-kg-lef004"),
        (8, "https://www.yesillimon.com/enterdonat-limon--1kg"),

        // LAMAS LİMON (9) - klasik sarı limon
        (9, "https://www.migros.com.tr/limon-kg-p-19ff462"),
        (9, "https://www.carrefoursa.com/limon-lamas-kg-p-30088937"),
        (9, "https://www.macrocenter.com.tr/limon-kg-p-1a013d5"),
        (9, "https://www.sokmarket.com.tr/limon-kg-p-5855/"),
        (9, "https://www.yesillimon.com/sari-limon"),
        (9, "https://www.gurmar.com.tr/limon-kg"),
        (9, "https://www.tazedukkan.com.tr"),
        (9, "https://www.sariyermarket.com/limon-kg"),
        (9, "https://www.hataykoy.com/urun/limon-3000-gr"),
        (9, "https://www.avokadocuayse.com/urunlerimiz/limon-1-kg/"),
        (9, "https://www.entazem.com/limon-500-gr"),

        // ══════════════════════════════════════════════════════
        // MANDALİNA ÇEŞİTLERİ
        // ══════════════════════════════════════════════════════

        // OKİTSU (1)
        (1, "https://www.naciyehanimciftligi.com/okitsu-mandalina-5-kg"),
        (1, "https://www.turkuazkoy.com/okitsu-mandalina-4kg"),

        // SATSUMA (2)
        (2, "https://www.migros.com.tr/mandalina-naturel-kg-p-1a05de4"),
        (2, "https://www.carrefoursa.com/mandalina-satsuma-izmir-kg-p-30087934"),
        (2, "https://www.macrocenter.com.tr/mandalina-naturel-kg-p-1a05e1f"),
        (2, "https://www.macrocenter.com.tr/mandalina-satsuma-kg-p-1a05e20"),
        (2, "https://www.tazedukkan.com.tr/gumuldur-satsuma-mandalina-5-kg-urun932.html"),
        (2, "https://www.sariyermarket.com/mandalina-natural-kg"),
        (2, "https://www.sariyermarket.com/mandalina-izmir-kg"),
        (2, "https://www.sebzemeyvedunyasi.com/mandalina-izmir-kg"),

        // KLEMANTİN (3)
        (3, "https://www.tazedirekt.com/klemantin-mandalina-1-kg-p-1a05e1c"),
        (3, "https://www.turkuazkoy.com/klemantin-4-kg"),

        // MURCOTT (5)
        (5, "https://www.migros.com.tr/mandalina-murcot-p-1a05de5"),
        (5, "https://www.carrefoursa.com/murcot-mandalina-kg-p-30113641"),
        (5, "https://www.carrefoursa.com/mandalina-kg-p-30113648"),
        (5, "https://www.macrocenter.com.tr/mandalina-murcot-kg-p-1a05e1e"),
        (5, "https://www.portakalbahcem.com/murcott-mandalina-1-kg-mmf001"),
        (5, "https://www.portakalbahcem.com/murcott-mandalina-3-kg-mmf002"),
        (5, "https://www.nuribeyciftligi.com/urun/w-murcott-mandalina-1kg/"),
        (5, "https://www.naciyehanimciftligi.com/w-murcott-mandalina-1-kg"),
        (5, "https://www.sariyermarket.com/mandalina-murcot-kg"),
        (5, "https://www.sebzemeyvedunyasi.com/mandalina-murkot--kg"),
        (5, "https://soyleyerinden.com/meyve/w.-murcott-mandalina-3-kg"),
        (5, "https://www.fresh.com.tr/urun/mnv-mandalina-murcott-kg"),
        (5, "https://www.entazem.com/mandalina-murcot"),
        (5, "https://www.yesillimon.com/mandalina"),
        (5, "https://www.anamurbahcesi.com/urun/mandalina-1-kg"),
        (5, "https://www.macrocenter.com.tr/organik-mandalina-p-1a04675"),
        (5, "https://www.tazedirekt.com/organik-mandalina-500-g-p-1a04677"),

        // ══════════════════════════════════════════════════════
        // AVOKADO
        // ══════════════════════════════════════════════════════

        // HASS (26) - pütürlü avokado
        (26, "https://sahidenorganik.com/product.php?slug=organik-avokado"),
        (26, "https://www.migros.com.tr/avokado-adet-p-1ab6614"),
        (26, "https://www.carrefoursa.com/avokado-adet-p-30009244"),
        (26, "https://www.carrefoursa.com/avokado-gurme-yemeye-hazir-adet-p-30082355"),
        (26, "https://www.macrocenter.com.tr/avokado-ekstra-adet-p-1ab7d2b"),
        (26, "https://www.macrocenter.com.tr/organik-avokado-adet-p-1ab6617"),
        (26, "https://www.sokmarket.com.tr/avokado-adet-p-36252"),
        (26, "https://www.portakalbahcem.com/puturlu-avokado-1-kg"),
        (26, "https://www.portakalbahcem.com/puturlu-avokado-5-adet-avf100"),
        (26, "https://www.portakalbahcem.com/avokado-1-kg"),
        (26, "https://www.avokadocuayse.com/urunlerimiz/hass-avokado-1-kg/"),
        (26, "https://www.yesillimon.com/avokado-1kg"),
        (26, "https://www.anamurbahcesi.com/urun/avokado"),
        (26, "https://www.eskitadinda.com/avokado-p"),
        (26, "https://www.tropiksepeti.com/urun/tropik-sepeti-hass-avokado-200"),
        (26, "https://www.avokadolu.com/yerli-avokado"),
        (26, "https://www.tazedukkan.com.tr/avokado-hass-cinsi-3-adet"),
        (26, "https://www.fresh.com.tr/urun/mnv-avokado-adet"),

        // FUERTE (29)
        (29, "https://www.avokadocuayse.com/urunlerimiz/fuerte-avokado-1-kg/"),
        (29, "https://www.avokadocuayse.com/urunlerimiz/fuerte-avokado-1-adet/"),

        // CLİFFTON (30)
        (30, "https://www.portakalbahcem.com/clifton-avokado-3-kg"),
        (30, "https://www.tazedukkan.com.tr/yerli-avokado-clifton-cinsi-3-adet"),

        // ══════════════════════════════════════════════════════
        // HİCAZ NAR (38)
        // ══════════════════════════════════════════════════════
        (38, "https://www.migros.com.tr/nar-kg-p-1a06990"),
        (38, "https://www.carrefoursa.com/nar-kg-p-30054444"),
        (38, "https://www.macrocenter.com.tr/nar-kg-p-1a069e0"),
        (38, "https://www.sokmarket.com.tr/nar-kg-p-3521/"),
        (38, "https://www.portakalbahcem.com/hicaz-nar-1-kg-nhf002"),
        (38, "https://www.portakalbahcem.com/hicaz-nar-3-kg-nhf001"),
        (38, "https://www.yesillimon.com/yerli-nar"),
        (38, "https://www.gurmar.com.tr/nar-kg-"),
        (38, "https://www.tazedirekt.com/organik-nar-1-kg-p-1a069df"),
        (38, "https://www.eskitadinda.com/nar-p"),
        (38, "https://www.sebzemeyvedunyasi.com/nar"),
        (38, "https://www.gulenbahce.com/urun/dogal-nar-1-kg/"),

        // ══════════════════════════════════════════════════════
        // KUMKUAT / LİMEKUAT
        // ══════════════════════════════════════════════════════

        // KUMKUAT (32)
        (32, "https://www.migros.com.tr/kumkuat-kg-p-1a43a45"),
        (32, "https://www.macrocenter.com.tr/excelente-kumkuat-paket-150-g-p-1a21750"),
        (32, "https://www.tazedirekt.com/organik-kumkuat-250-g-p-1a0ac46"),
        (32, "https://www.carrefoursa.com/kumkuat-250-g-p-30240470"),
        (32, "https://www.portakalbahcem.com/kumkuat-500-gr-ekf201"),
        (32, "https://www.portakalbahcem.com/kumkuat-1-kg-ekf202"),
        (32, "https://www.portakalbahcem.com/kumkuat-3-kg-ekf203"),
        (32, "https://www.naciyehanimciftligi.com/kumkuat"),
        (32, "https://www.gurmar.com.tr/kumkuat-kg"),
        (32, "https://www.anamurbahcesi.com/urun/kumkuat"),
        (32, "https://www.hediyelikbahcem.com/1kg-kumkuat"),
        (32, "https://dalindanlezzet.com/product/kumkuat-kumkat-kamkat/"),
        (32, "https://www.enmanav.com/kumkuat-500-g"),
        (32, "https://www.eskitadinda.com/kumkuat-p"),
        (32, "https://www.gulenbahce.com/urun/dogal-kumkuat-1-kg/"),
        (32, "https://www.tropiksepeti.com/urun/kumkuat-500-gr-1"),
        (32, "https://www.sebzemeyvedunyasi.com/kumkuat"),
        (32, "https://www.avokadocuayse.com/urunlerimiz/kamkat-kumkuat-500-gram/"),

        // LİMEKUAT (36)
        (36, "https://www.portakalbahcem.com/limekuat-500-gr-elf204"),
        (36, "https://www.portakalbahcem.com/limekuat-1-kg-elf205"),
        (36, "https://www.portakalbahcem.com/limekuat-3-kg-elf206"),
        (36, "https://www.tropiksepeti.com/urun/limequat-500-gr-324"),

        // ══════════════════════════════════════════════════════
        // ŞADOK (33)
        // ══════════════════════════════════════════════════════
        (33, "https://www.portakalbahcem.com/sadok-esf-001"),
        (33, "https://www.entazem.com/pomelo-greyfurt-adet"),
        (33, "https://nuribeyciftligi.com/agac-kavunu-sadok-1-2kg"),

        // ══════════════════════════════════════════════════════
        // TURUNÇ (37) / BERGAMOT (31)
        // ══════════════════════════════════════════════════════
        (37, "https://www.portakalbahcem.com/turunc-2-kg-etf001"),
        (37, "https://www.sebzemeyvedunyasi.com/turunc"),
        (37, "https://www.organiklimon.com/urun/turunc-2kg-kutu"),

        (31, "https://www.portakalbahcem.com/bergamot-1-kg"),
        (31, "https://www.portakalbahcem.com/bergamot-3-kg"),
        (31, "https://www.lokmaciana.com/urun/bergamot"),

        // ══════════════════════════════════════════════════════
        // GREYFURT
        // ══════════════════════════════════════════════════════

        // KIRMIZI GREYFURT (35)
        (35, "https://www.migros.com.tr/greyfurt-kan-kg-p-19e7590"),
        (35, "https://www.carrefoursa.com/kanli-greyfurt-kg-p-30030138"),
        (35, "https://www.macrocenter.com.tr/greyfurt-kan-kg-p-19e9101"),
        (35, "https://www.sokmarket.com.tr/greyfurt-kg-p-6056/"),
        (35, "https://www.portakalbahcem.com/kirmizi-greyfurt-1-kg"),
        (35, "https://www.portakalbahcem.com/kirmizi-greyfurt-3-kg-gkf003"),
        (35, "https://www.yesillimon.com/kirmizi-greyfurt"),
        (35, "https://www.tazedirekt.com/organik-kan-greyfurt-1-kg-p-19e90ff"),
        (35, "https://www.naciyehanimciftligi.com/kirmizi-greyfurt-1kg"),
        (35, "https://www.anamurbahcesi.com/urun/organik-altintop-greyfurt-1-kg"),
        (35, "https://www.gurmar.com.tr/greyfurt-kg"),
        (35, "https://www.sariyermarket.com/greyfurt-kg"),
        (35, "https://www.damlicaciftligi.com/greyfurt-kirmizi-1-kg"),
        (35, "https://www.sebzemeyvedunyasi.com/sari-greyfurt-kg"),
        (35, "https://www.entazem.com/greyfurt"),

        // BEYAZ GREYFURT (34)
        (34, "https://www.yesillimon.com/beyaz-greyfurt"),

        // ══════════════════════════════════════════════════════
        // LIME ÇEŞİTLERİ
        // ══════════════════════════════════════════════════════

        // LIME (13)
        (13, "https://www.carrefoursa.com/misket-limonu-3-lu-paket-p-30082444"),
        (13, "https://www.macrocenter.com.tr/yerli-misket-limon-lime-350-g-paket-adet-p-1a013d3"),
        (13, "https://www.macrocenter.com.tr/excelente-lime-3lu-paket-195-g-p-1a48843"),
        (13, "https://www.tazedirekt.com/lime-limon-3lu-paket-195-g-p-1a48843"),
        (13, "https://www.portakalbahcem.com/lime-500-gr-elf101"),
        (13, "https://www.portakalbahcem.com/lime-1-kg-elf102"),
        (13, "https://www.yesillimon.com/yerli-lime---500-gr"),
        (13, "https://www.anamurbahcesi.com/urun/lime-limon-500-gr"),
        (13, "https://www.gulenbahce.com/urun/lime-misket-limon-yesil-limon-500-gr/"),
        (13, "https://www.tropiksepeti.com/urun/lime-limon-500-gr"),
        (13, "https://www.nuribeyciftligi.com/urun/lime/"),

        // KAFFİR LIME (14)
        (14, "https://www.portakalbahcem.com/kaffir-lime-500-gr-ekf102"),
        (14, "https://www.portakalbahcem.com/kaffir-lime-1-kg"),

        // FINGER LIME - VERDE (17), ROSE (18), SUNPEARL (19)
        (17, "https://www.portakalbahcem.com/verde-havyar-limon-500-gr"),
        (18, "https://www.portakalbahcem.com/rose-havyar-limon-500-gr"),
        (19, "https://www.portakalbahcem.com/ilk-hasat-sunpearl-havyar-limon-500-gr"),

        // ══════════════════════════════════════════════════════
        // EJDER MEYVESİ
        // ══════════════════════════════════════════════════════

        // KIRMIZI EJDER (40)
        (40, "https://www.carrefoursa.com/pitahaya-ejder-meyvesi-adet-p-30082440"),
        (40, "https://www.portakalbahcem.com/ejder-meyvesi-pitaya-1-kg"),
        (40, "https://www.portakalbahcem.com/ejder-meyvesi-pitaya-2-kg"),
        (40, "https://www.yesillimon.com/pitaya-ejder-meyvesi"),
        (40, "https://www.anamurbahcesi.com/urun/ejder-meyvesi-pitaya-1-adet-300-gr"),
        (40, "https://www.tazedukkan.com.tr/ejder-meyvesi-1-adet-250-300-gr"),
        (40, "https://www.tropiksepeti.com/urun/ejder-meyvesi-1-adet"),
        (40, "https://www.sebzemeyvedunyasi.com/ejder-meyvesi-pitaya"),
        (40, "https://www.enmanav.com/ejder-meyvesi-adet"),
        (40, "https://www.meyvebahceniz.com/ejder-meyvesi-pitahaya"),
        (40, "https://www.avokadocuayse.com/urunlerimiz/1-adet-kirmizi-ejder-meyvesi/"),
        (40, "https://www.kaptantarim.com.tr/kirmizi-ejder-pitaya-meyvesi"),

        // ══════════════════════════════════════════════════════
        // ÇARKIFELEK (42)
        // ══════════════════════════════════════════════════════
        (42, "https://www.carrefoursa.com/carkifelek-130-g-p-30092623"),
        (42, "https://www.portakalbahcem.com/carkifelek-500-gr-crf001"),
        (42, "https://www.portakalbahcem.com/carkifelek-1-kg-crf002"),
        (42, "https://www.macrocenter.com.tr/verita-carkifelek-passion-fruit-3lu-paket-p-1a4fd8c"),
        (42, "https://www.tropiksepeti.com/urun/carkifelek-500-gr"),
        (42, "https://www.tropiksepeti.com/urun/carkifelek-1-kg"),
        (42, "https://www.anamurbahcesi.com/urun/carkifelek-4-adet"),
        (42, "https://soyleyerinden.com/taze%20meyve/carkifelek-meyvesi-passiflora-1-kg"),
        (42, "https://www.sebzemeyvedunyasi.com/carkifelek"),
        (42, "https://www.avokadocuayse.com/urunlerimiz/passifloracarkifelek-meyvesi/"),
        (42, "https://www.enmanav.com/passion-fruit-4lu-paket1"),
        (42, "https://kaptantarim.com.tr/passiflora-carkifelek-meyvesi"),

        // ══════════════════════════════════════════════════════
        // MANGO (44)
        // ══════════════════════════════════════════════════════
        (44, "https://www.carrefoursa.com/mango-adet-p-30009345"),
        (44, "https://www.macrocenter.com.tr/excelente-yemeye-hazir-mango-adet-p-1a21744"),
        (44, "https://www.macrocenter.com.tr/verita-yemeye-hazir-mango-adet-p-1a4e9fa"),
        (44, "https://www.portakalbahcem.com/mango-1-kg"),
        (44, "https://www.portakalbahcem.com/mango-3-kg"),
        (44, "https://www.tropiksepeti.com/urun/mango-1-adet-yerli-300-400-gr-702"),
        (44, "https://www.sebzemeyvedunyasi.com/mango"),
        (44, "https://www.enmanav.com/mango-adet"),
        (44, "https://www.avokadocuayse.com/urunlerimiz/yerli-mango-1-kilo/"),
        (44, "https://www.tazedukkan.com.tr/mango-adet"),

        // ══════════════════════════════════════════════════════
        // DOMATES
        // ══════════════════════════════════════════════════════

        // SALKIMLI / PEMBE DOMATES (47)
        (47, "https://sahidenorganik.com/product.php?slug=organik-pembe-domates"),
        (47, "https://www.migros.com.tr/domates-pembe-kg-p-1ac9aca"),
        (47, "https://www.carrefoursa.com/pembe-domates-kg-p-30126614"),
        (47, "https://www.carrefoursa.com/domates-pazar-kg-p-30013008"),
        (47, "https://www.sokmarket.com.tr/domates-kg-p-3855"),
        (47, "https://www.macrocenter.com.tr/domates-pembe-kg-p-1ac9b41"),
        (47, "https://www.macrocenter.com.tr/domates-kg-p-1ac9b3c"),
        (47, "https://www.macrocenter.com.tr/organik-pembe-domates-kg-p-1ac77a4"),
        (47, "https://www.portakalbahcem.com/pembe-domates-2-kg-dpg001"),
        (47, "https://www.gurmar.com.tr/domates-pembe-kg-"),
        (47, "https://www.eskitadinda.com/pembe-domates-p"),
        (47, "https://www.anamurbahcesi.com/urun/pembe-domates"),
        (47, "https://www.sebzemeyvedunyasi.com/-iyi-tarim-pembe-domates-kg"),
        (47, "https://www.buyukannem.com/urun/domates-pembe/"),
        (47, "https://www.entazem.com/pembe-domates-500-g"),

        // LİMON OTU (50)
        (50, "https://www.yesillimon.com/limon-otu---100-gr"),
        (50, "https://egepazarindan.com/limon-otu-50-gr-lemon-grass/"),

        // ══════════════════════════════════════════════════════
        // EXCEL'DEN EKLENEN URL'LER (120 yeni)
        // ══════════════════════════════════════════════════════

        // SATSUMA (2)
        (2, "https://www.anamurbahcesi.com/urun/mandalina-10-kg"),
        (2, "https://www.gurmar.com.tr/mandalina-kg"),
        (2, "https://www.carrefoursa.com/organik-mandalina-kg-p-30097505"),
        (2, "https://www.macrocenter.com.tr/organik-mandalina-kg-p-1a04677"),
        (2, "https://www.tazedukkan.com.tr/gumuldur-luks-satsuma-mandalina-5-kg-olgun-hasat"),
        (2, "https://www.tazedukkan.com.tr/gumuldur-luks-satsuma-mandalina-10-kg-"),
        (2, "https://www.sebzemeyvedunyasi.com/yesil-mandalina-kg"),
        (2, "https://www.hediyelikbahcem.com/10kg-satsuma-clausellina-mandalina-2"),

        // MAYER LİMON (6)
        (6, "https://toplasepeti.com/mayer-limon-5-kg"),

        // LAMAS LİMON (9)
        (9, "https://www.yesillimon.com/yesil-limon-1kg"),
        (9, "https://www.tropiksepeti.com/urun/limon"),
        (9, "https://www.organikgiller.com/product-page/organik-limon-kg"),
        (9, "https://www.macrocenter.com.tr/tazedirekt-limon-organik-kg-p-1a05dd9"),
        (9, "https://www.macrocenter.com.tr/organik-limon-kg-p-19ff466"),
        (9, "https://olbagro.com/urun/5-kg-dalindan-dogal-kutdiken-limon"),

        // LIME (13)
        (13, "https://www.tropiksepeti.com/urun/lime-limon-1-kg"),
        (13, "https://www.macrocenter.com.tr/verita-limes-3-lu-pkt-p-1a013c8"),
        (13, "https://tazegel.com/urun/limon-lime-1-kasa-8-kg"),
        (13, "https://www.sebzemeyvedunyasi.com/lime-limon"),
        (13, "https://www.dunyaninmeyvesi.com/lime-3lu-paket"),
        (13, "https://soyleyerinden.com/yoresinden-hemen-soyle/akdeniz/lime-5-kg"),

        // WASHINGTON PORTAKAL (21)
        (21, "https://www.istegelsin.com/urun/portakal-1-kg_MS50-AD"),
        (21, "https://www.fresh.com.tr/urun/portakal-washington-kg"),
        (21, "https://www.fresh.com.tr/urun/portakal-cavdir-kg"),
        (21, "https://www.turkuazkoy.com/sikmalik-finike-portakali"),
        (21, "https://www.fresh.com.tr/urun/mv-portakal-sikma-kg"),
        (21, "https://www.portakalbahcem.com/ilk-hasat-finike-portakali-yemelik-washington-3-kg-pwf002"),
        (21, "https://www.migros.com.tr/m-life-organik-portakal-kg-paket-ort-520-g-p-1a0abfa"),
        (21, "https://www.portakalbahcem.com/finike-portakali-sikmalik-washington-3-kg-pwf007"),
        (21, "https://www.macrocenter.com.tr/tazedirekt-portakal-organik-kg-p-1a0abf8"),
        (21, "https://www.macrocenter.com.tr/organik-portakal-sikma-kg-p-1a090a2"),
        (21, "https://ardenmarket.com.tr/catalog/product/view/id/1859/s/manav-portakal-finike-lux-kg-1"),
        (21, "https://buyukannem.com/urun/portakal"),
        (21, "https://www.datcamuratciftligi.com/urun/organik-sikmalik-portakal-paketi-3-kg-1"),
        (21, "https://www.sebzemeyvedunyasi.com/cavdir-portakal-kg"),
        (21, "https://www.turuncubag.com/yafa-portakali-13.5-kg"),
        (21, "https://www.portakalbahcem.com/yafa-portakal-3-kg-pyf001"),

        // VALENSİYA PORTAKAL (22)
        (22, "https://www.gulenbahce.com/urun/valencia-yerli-portakal-sulu-dogal-10-kg"),
        (22, "https://www.macrocenter.com.tr/portakal-organik-kg-p-1a0ac4f"),
        (22, "https://www.gurmar.com.tr/portakal-valencia-kg"),
        (22, "https://www.pazarama.com/kb-koycegiz-yoresel-koycegiz-portakali-valensiya-sikmalik-8-kg-p-8680030581131"),
        (22, "https://online.metro-tr.com/portakal-yemelik-p-282624"),

        // MYSTİCRİMSON KAN PORTAKAL (23)
        (23, "https://turuncubag.com/urun/kan-portakali-3-kg"),
        (23, "https://nuribeyciftligi.com/portakal-cara-cara-3kg"),

        // HASS AVOKADO (26)
        (26, "https://www.carrefoursa.com/avokado-adet-p-30281297"),
        (26, "https://www.tazedukkan.com.tr/yerli-finike-avokado-10-adet-urun1781.html"),
        (26, "https://www.tazemasa.com/5li-avokado-gazipasaantalya-5368"),
        (26, "https://www.portakalbahcem.com/avokado-5-adet-avf004"),
        (26, "https://www.portakalbahcem.com/avokado-8-adet-avf005"),
        (26, "https://www.portakalbahcem.com/avokado-14-adet-avf006"),
        (26, "https://www.yesillimon.com/avokado-zutano---1-kg"),
        (26, "https://www.portakalbahcem.com/puturlu-avokado-8-adet-avf101"),
        (26, "https://www.portakalbahcem.com/puturlu-avokado-14-adet-avf102"),
        (26, "https://www.tazedukkan.com.tr/talat-celik-organik-sertifikali-avokado"),
        (26, "https://www.avokadocuayse.com/urunlerimiz/hass-avokado-160-gram-ve-uzeri"),
        (26, "https://www.portakalbahcem.com/puturlu-avokado-3-kg"),
        (26, "https://www.portakalbahcem.com/puturlu-avokado-1-5-kg"),
        (26, "https://www.tropiksepeti.com/urun/tropik-sepeti-hass-avokado-15-adet-199"),
        (26, "https://www.tropiksepeti.com/urun/clifton-avokado-1-adet-100-350gr-709-1"),
        (26, "https://www.tropiksepeti.com/urun/avokado-20-adet"),
        (26, "https://www.avokadolu.com/yerli-avokado-24-adet"),
        (26, "https://www.avokadolu.com/yerli-avokado-48-adet"),
        (26, "https://www.portakalbahcem.com/avokado-3-kg"),
        (26, "https://www.portakalbahcem.com/avokado-1-5-kg"),
        (26, "https://www.macrocenter.com.tr/avokado-adet-p-1ab6625"),
        (26, "https://www.tropiksepeti.com/urun/avokado-10-adet"),

        // BACON AVOKADO (28)
        (28, "https://www.anamurbahcesi.com/urun/organik-bacon-avokado-4-adet"),
        (28, "https://www.yesillimon.com/avokado-bacon-1-kg"),

        // FUERTE AVOKADO (29)
        (29, "https://www.anamurbahcesi.com/urun/4-adet-organik-avokado"),
        (29, "https://www.avokadocuayse.com/urunlerimiz/fuerte-avokado"),
        (29, "https://www.avokadocuayse.com/urunlerimiz/buyuk-boy-fuerte-avokado"),

        // KUMKUAT (32)
        (32, "https://gurmar.com.tr/kumkuat-kg"),
        (32, "https://www.tazedukkan.com.tr/kumkuat-200gr-paket"),
        (32, "https://www.fresh.com.tr/urun/mnv-kumkuat-kg"),
        (32, "https://www.istegelsin.com/urun/kumkuat-200-g_MS318-AD"),

        // ŞADOK (33)
        (33, "https://www.hepsiburada.com/sebze-meyve-dunyasi-pomelo-p-HBCV000011WJJZ"),
        (33, "https://panayirgourmet.com/products/pomelo"),

        // KIRMIZI GREYFURT (35)
        (35, "https://www.portakalbahcem.com/kirmizi-greyfurt-2-5-kg-gkf001"),
        (35, "https://toplasepeti.com/greyfurt-1-kg"),
        (35, "https://www.tazemasa.com/organik-greyfurt-kg--937"),
        (35, "https://www.eskitadinda.com/greyfurt-p"),
        (35, "https://www.portakalbahcem.com/sikmalik-kirmizi-greyfurt-3-kg"),
        (35, "https://tazegel.com/urun/greyfurt"),
        (35, "https://www.tazemasa.com/greyfurt-mersin-kg-20"),

        // TURUNÇ (37)
        (37, "https://www.n11.com/urun/dogalkasa-taze-turunc-3-kg-1633764"),
        (37, "https://www.organiklimon.com/urun/turunc-15kg-kutu"),

        // HİCAZ NAR (38)
        (38, "https://www.tazemasa.com/nar-finike-kg-29"),
        (38, "https://www.macrocenter.com.tr/organik-nar-kg-p-1a0699d"),
        (38, "https://www.tazedirekt.com/organik-sikmalik-nar-1-kg-p-1a069a6"),
        (38, "https://www.portakalbahcem.com/sikmalik-hicaz-nar-3-kg-nhf003"),
        (38, "https://www.fresh.com.tr/urun/sikmalik-nar-kg"),
        (38, "https://www.carrefoursa.com/nar-organik-kg-p-30091647"),
        (38, "https://www.macrocenter.com.tr/tazedirekt-organik-nar-kg-p-1a069df"),
        (38, "https://www.sariyermarket.com/nar-eksi-kg"),
        (38, "https://www.gurmekoy.com/nar-finike-kg"),
        (38, "https://www.enmanav.com/nar-kg1"),

        // KIRMIZI EJDER MEYVESİ (40)
        (40, "https://tropikalturkiye.com/urun/ejder-meyvesi"),
        (40, "https://www.yesillimon.com/pitaya-ejdermeyvesi"),
        (40, "https://www.hepsiburada.com/kuzey-tropik-yerli-ejder-meyvesi-1-adet-pm-HBC00004HQQUY"),
        (40, "https://www.hepsiburada.com/sebze-meyve-dunyasi-ejder-meyvesi-pitahaya-1-adet-pm-HBC00000ULTPN"),
        (40, "https://soyleyerinden.com/meyve/kirmizi-ejder-meyvesi-1-kg"),
        (40, "https://tazegel.com/urun/ejder-meyvesi-kirmizi"),
        (40, "https://www.avokadocuayse.com/urunlerimiz/1-adet-beyaz-ejder-meyvesi"),

        // MOR ÇARKIFELEK (42)
        (42, "https://www.anamurbahcesi.com/urun/carkifelek-2-adet"),
        (42, "https://www.tazemasa.com/passion-fruit-carkifelek-excelente-5664"),
        (42, "https://www.tazedukkan.com.tr/carkifelek-meyvesi-3lu-paket-"),
        (42, "https://www.tazemasa.com/carkifelek-passion-fruit-3-lu-paket-73"),
        (42, "https://www.tropiksepeti.com/urun/carkifelek-4-adet-401"),
        (42, "https://www.tropiksepeti.com/urun/aroma-dolu-olgun-carkifelek-meyvesi-4-adet-698"),
        (42, "https://soyleyerinden.com/meyve-sebze/carkifelek-passion-fruit-3-kg-45-50-adet"),

        // KENT MANGO (44)
        (44, "https://www.tropiksepeti.com/urun/mango-1-adet-yerli-750-1000-gr"),
        (44, "https://www.avokadocuayse.com/urunlerimiz/1-adet-yerli-mango-500-700-gram"),
        (44, "https://www.dunyaninmeyvesi.com/yemeye-hazir-mango-paket"),

        // SALKIMLI DOMATES (47)
        (47, "https://panayirgourmet.com/products/pembe-domates"),
        (47, "https://www.macrocenter.com.tr/tazedirekt-organik-domates-kg-koy-p-1ac9aaf"),
        (47, "https://www.macrocenter.com.tr/tazedirekt-pembe-domates-kg-p-1ac9b4c"),
        (47, "https://buyukannem.com/urun/domates-pembe"),
        (47, "https://www.nuhunambari.com/Organik-Pembe-Domates-kg,PR-1927.html"),

        // LİMON OTU (50)
        (50, "https://mutlusebzeler.com/urun/limon-otu"),
        (50, "https://www.tosmagaza.com/limon-otu-50-gr"),

        // ══════════════════════════════════════════════════════
        // REÇEL & MARMELAT
        // ══════════════════════════════════════════════════════

        // PORTAKAL REÇELİ (52)
        (52, "https://www.portakalbahcem.com/portakal-receli-290-gr"),
        (52, "https://www.migros.com.tr/yenigun-seker-ilavesiz-portakal-kabugu-receli-290-g-p-89b2bc"),
        (52, "https://www.yenigunstore.com/seker-ilavesiz-portakal-kabugu-receli-290-gr"),
        (52, "https://sekerhanim.com/urun/seker-ilavesiz-urunler/seker-ilavesiz-portakal-receli-300-gr"),
        (52, "https://www.antalyarecelcisi.com/urun/portakal-kabugu-receli-370g-rafine-seker-ilavesiz"),
        (52, "https://www.antalyarecelcisi.com/urun/antalya-recelcisi-portakal-kabugu-receli-290g-rafine-seker-ilavesiz-2709"),
        (52, "https://bodrumyadigari.com/recel-marmelat/receller/prd-seker-ilavesiz-portakal-receli-250gr"),
        (52, "https://www.polsgurme.com/polsgurme-portakal-receli-chia-tohumlu-seker-ilavesiz-e-meyve-oranli-380-gr"),

        // TURUNÇ REÇELİ (53)
        (53, "https://www.portakalbahcem.com/turunc-receli-290-gr"),
        (53, "https://www.yenigunstore.com/seker-ilavesiz-turunc-kabugu-receli-290-gr"),
        (53, "https://www.antalyarecelcisi.com/urun/turunc-kabugu-receli-370g-rafine-seker-ilavesiz"),
        (53, "https://www.antalyarecelcisi.com/urun/antalya-recelcisi-turunc-kabugu-receli-290g-rafine-seker-ilavesiz-2697"),
        (53, "https://sekerhanim.com/urun/seker-ilavesiz-urunler/seker-ilavesiz-turunc-receli-300-gr"),
        (53, "https://bodrumyadigari.com/recel-marmelat/receller/prd-seker-ilavesiz-turunc-receli-250gr"),
        (53, "https://tazedukkan.com.tr/seker-ilavesiz-turunc-receli-300-gr-urun552.html"),

        // BERGAMOT REÇELİ (54)
        (54, "https://www.portakalbahcem.com/bergamot-receli-290-gr"),
        (54, "https://www.yenigunstore.com/seker-ilavesiz-bergamot-kabugu-receli-290-gr"),
        (54, "https://www.antalyarecelcisi.com/urun/bergamot-kabugu-receli-370g-rafine-seker-ilavesiz"),
        (54, "https://sekerhanim.com/urun/seker-ilavesiz-urunler/seker-ilavesiz-bergamot-receli-300-gr"),
        (54, "https://bodrumyadigari.com/recel-marmelat/receller/prd-seker-ilavesiz-bergamot-receli-250gr"),

        // LİMON REÇELİ (55)
        (55, "https://www.portakalbahcem.com/limon-receli-290-gr"),
        (55, "https://www.migros.com.tr/yenigun-seker-ilavesiz-limon-kabugu-receli-290-g-p-89b2bd"),
        (55, "https://www.yenigunstore.com/seker-ilavesiz-limon-kabugu-receli-290-gr"),
        (55, "https://www.antalyarecelcisi.com/urun/limon-kabugu-receli-370g-rafine-seker-ilavesiz"),
        (55, "https://www.antalyarecelcisi.com/urun/antalya-recelcisi-limon-kabugu-receli-290g-rafine-seker-ilavesiz-2710"),
        (55, "https://bodrumyadigari.com/recel-marmelat/receller/prd-seker-ilavesiz-limon-receli-250gr"),

        // KUMKUAT REÇELİ (56)
        (56, "https://www.portakalbahcem.com/kumkuat-receli-290-gr"),

        // NAR REÇELİ (57)
        (57, "https://www.portakalbahcem.com/nar-receli-290-gr"),
        (57, "https://www.polsgurme.com/polsgurme-nar-receli-chia-tohumlu-seker-ilavesiz-e-meyve-oranli-380-gr"),

        // MANDALİNA MARMELATI (58)
        (58, "https://www.portakalbahcem.com/mandalina-receli-290-gr"),
        (58, "https://www.yenigunstore.com/yenigun-seker-ilavesiz-mandalina-receli-290-gr"),
        (58, "https://sekerhanim.com/urun/seker-ilavesiz-urunler/seker-ilavesiz-mandalina-marmelati-300-gr"),
        (58, "https://tazedukkan.com.tr/seker-hanim-mandalina-marmelati-300-gr-urun1632.html"),

        // KARIŞIK MARMELAT (59)
        (59, "https://www.portakalbahcem.com/seker-ilavesiz-erik-marmelati-290-gr-rl-128"),
        (59, "https://www.portakalbahcem.com/seker-ilavesiz-armut-marmelati-290-gr-rl-127"),
        (59, "https://www.portakalbahcem.com/seker-ilavesiz-carkifelek-marmelati-290-gr-rl-119"),
        (59, "https://sekerhanim.com/urun/seker-ilavesiz-urunler/seker-ilavesiz-murdum-erik-receli-300-gr"),
        (59, "https://www.polsgurme.com/polsgurme-erik-receli-chia-tohumlu-seker-ilavesiz-e-meyve-oranli-380-gr"),

        // ══════════════════════════════════════════════════════
        // NAR EKŞİSİ (60)
        // ══════════════════════════════════════════════════════
        (60, "https://www.portakalbahcem.com/nar-eksisi-330-gr"),
        (60, "https://www.fresh.com.tr/urun/sade-330-gr-organik-nar-eksisi"),
        (60, "https://www.migros.com.tr/kuhne-100-nar-eksisi-250-ml-p-fa6999"),
        (60, "https://www.macrocenter.com.tr/punica-sadece-nar-eksisi-250-ml-p-5c1688"),
        (60, "https://www.yesillimon.com/hakiki-nar-eksisi-350-gr"),
        (60, "https://www.ancora.com.tr/urun/nar-eksisi-350gr"),
        (60, "https://www.marketpaketi.com.tr/ancora-organik-100-nar-eksisi-350-gr-p-946"),
        (60, "https://www.beyorganik.com/organik-nar-eksisi-250ml"),
        (60, "https://www.sofyaninarkabahcesi.com/nar-eksisi-hakiki-organik"),
        (60, "https://www.sofyaninarkabahcesi.com/nar-eksisi-hakiki-organik-700cc"),
        (60, "https://www.arifoglu.com/100-nar-eksisi-238-ml-320-g"),
        (60, "https://www.degirmencidede.com/urun/nar-eksisi-ev-yapimi-katkisiz"),
        (60, "https://www.yoreselyasam.com/urun/ev-yapimi-nar-eksisi"),
        (60, "https://www.dogalyasam.net/sultan-gazi-nar-eksisi-dogal-ev-yapimi"),
        (60, "https://bodrumyadigari.com/sos-pekmez-sirke/prd-nar-eksisi-1000ml"),
        (60, "https://bodrumyadigari.com/sos-pekmez-sirke/prd-nar-eksisi-200ml"),
        (60, "https://www.sariyermarket.com/nar-eksi-kg"),

        // ══════════════════════════════════════════════════════
        // KURUTULMUŞ ÜRÜNLER
        // ══════════════════════════════════════════════════════

        // KURUTULMUŞ NARENÇİYE (61)
        (61, "https://egepazarindan.com/kurutulmus-portakal-100-gr-koycegiz-portakal-cipsi"),
        (61, "https://egepazarindan.com/mandalina-cipsi-100-gr-kurutulmus-koycegiz-mandalinasi"),
        (61, "https://www.yesillimon.com/portakal-kurusu-100-gr"),
        (61, "https://www.nuribeyciftligi.com/urun/kurutulmus-portakal-dilimleri"),
        (61, "https://www.nuribeyciftligi.com/urun/kurutulmus-limon-dilimleri"),
        (61, "https://www.datcamuratciftligi.com/urun/portakal-kurusu-50-gr"),
        (61, "https://www.datcamuratciftligi.com/urun/mandalina-kurusu-50-gr"),
        (61, "https://www.portakalbahcem.com/kurutulmus-portakal-dilimi-100gr"),
        (61, "https://www.altanaturel.com/urun/portakal-cipsi-100-gr"),
        (61, "https://www.altanaturel.com/urun/mandalina-cipsi-100-gr"),
        (61, "https://www.altanaturel.com/urun/greyfurt-cipsi-100-gr"),
        (61, "https://www.muskoshop.com/portakal-kurusu-kg"),
        (61, "https://www.muskoshop.com/mandalina-kurusu-kg"),
        (61, "https://www.muskoshop.com/limon-kurusu-kg"),
        (61, "https://www.muskoshop.com/greyfurt-kurusu-kg"),
        (61, "https://www.muskoshop.com/freeze-dry-portakal-20-gr"),
        (61, "https://www.muskoshop.com/freeze-dry-limon-20-gr"),
        (61, "https://www.kurual.com/kuru-portakal"),
        (61, "https://www.ozgurleblebi.com/Kuru-Portakal-Cips-Dogal"),
        (61, "https://www.egeyedonus.com/portakal-kurusu"),
        (61, "https://www.egeyedonus.com/limon-kurusu"),
        (61, "https://birfincan.com/urunler/kurutulmus-limon-dilimleri-100-gr"),
        (61, "https://www.onadeger.com/kurutulmus-limon-dilimleri-30-gr"),
        (61, "https://www.malatyapazaripalanci.com.tr/yerli-limon-kurusu-100-g"),
        (61, "https://www.fxfood.com.tr/greyfurt-kurusu-100-g"),
        (61, "https://www.umitkuruyemis.com.tr/tropikal-kuru-greyfurt"),
        (61, "https://unalkuruyemis.com.tr/kurutulmus-portakal-200-gr-paket"),
        (61, "https://magaza.hammaddeler.com/urun/kurutulmus-portakal-100-g"),
        (61, "https://bodrumyadigari.com/kurutulmus-urunler/meyve-kurulari/prd-nar-kurusu-100gr"),
        (61, "https://bodrumyadigari.com/kurutulmus-urunler/cipsler/prd-mandalin-cips-150-gr"),
        (61, "https://bodrumyadigari.com/kurutulmus-urunler/cipsler/prd-mandalin-cips-45gr"),
        (61, "https://bodrumyadigari.com/kurutulmus-urunler/cipsler/prd-mandalin-cips-25gr"),
        (61, "https://www.citlekci.com.tr/dogal-kurutulmus-limon-kurusu"),
        (61, "https://www.hepsiburada.com/dogal-kuru-limon-cips-kurusu-30g-pm-HBC00001MLZMQ"),

        // KAN PORTAKAL KURUSU (62)
        (62, "https://www.portakalbahcem.com/kurutulmus-kan-portakal-dilimi-100gr"),
        (62, "https://www.datcamuratciftligi.com/urun/kan-portakali-kurusu-50-g"),
        (62, "https://www.muskoshop.com/kan-portakal-kurusu-kg"),

        // NAR KURUSU (63)
        (63, "https://www.portakalbahcem.com/kurutulmus-nar-tanesi-300-gr-kk-022"),

        // KAFFİR LİME YAPRAĞI (64)
        (64, "https://www.portakalbahcem.com/kurutulmus-kaffir-lime-yapragi-50-gr"),
        (64, "https://www.otcubitki.com/urun/kaffir-lime-kaffir-limi-yaprak-20-g"),
        (64, "https://www.bizeikicay.com/urun/kabuk-bergamot"),

        // LİME KURUSU (65)
        (65, "https://www.portakalbahcem.com/kurutulmus-lime-dilimi-100-gr"),
        (65, "https://www.muskoshop.com/freeze-dry-lime-15-gr"),
        (65, "https://ciftcideneve.com/urun/6769/lime-kurusu-100-gr-dogal-organik-kuru-meyve"),
        (65, "https://www.soyleyerinden.com/meyve-kurusu/lime-kurusu-100-gr"),

        // ══════════════════════════════════════════════════════
        // MEYVE SUYU
        // ══════════════════════════════════════════════════════

        // NAR SUYU (66)
        (66, "https://www.portakalbahcem.com/taze-sikma-nar-950-ml"),
        (66, "https://www.portakalbahcem.com/taze-sikma-nar-6-200-ml"),
        (66, "https://www.fresh.com.tr/urun/ben-organik-250-ml-nar-suyu"),
        (66, "https://www.macrocenter.com.tr/exotic-nar-suyu-750-cc-p-7b2dda"),
        (66, "https://www.carrefoursa.com/naren-100-sikma-nar-suyu-250-ml-cam-sise-p-30252814"),
        (66, "https://www.elitenaturel.com/urun/organik-nar-suyu-200-ml"),
        (66, "https://www.nartalya.com/urun/nartalya-dondurulmus-nar-suyu-900-ml"),
        (66, "https://www.nartalya.com/urun/nartalya-dondurulmus-nar-suyu"),
        (66, "https://yorukana.com/yoruk-ana-geleneksel-0-nar-suyu-1-lt"),
        (66, "https://www.pttavm.com/nar-suyu-200-ml-p-61563270"),

        // LİMON SUYU (67)
        (67, "https://www.portakalbahcem.com/taze-sikma-limon-950-ml"),
        (67, "https://www.yesillimon.com/limonsuyu"),
        (67, "https://www.gurmeninyeri.com/tr/global-yuzde-100-dogal-limon-suyu-1-lt"),
    ];

    public static async Task EnsureSeededAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var markets = await db.Markets.ToListAsync(ct);
        var domainMap = BuildDomainMap(markets);

        var existingUrls = (await db.ProductMarketLinks
            .Select(l => l.DirectUrl)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<ProductMarketLink>();

        foreach (var (varietyId, rawUrl) in Links)
        {
            // URL'yi temizle (query string'i at)
            var url = StripQuery(rawUrl);
            if (string.IsNullOrWhiteSpace(url)) continue;
            if (existingUrls.Contains(url)) continue;

            var marketId = FindMarketId(domainMap, url);
            if (marketId is null) continue;

            toAdd.Add(new ProductMarketLink
            {
                ProductVarietyId = varietyId,
                MarketId = marketId.Value,
                DirectUrl = url,
                IsActive = true
            });
            existingUrls.Add(url);  // duplicate guard within this batch
        }

        if (toAdd.Count > 0)
        {
            db.ProductMarketLinks.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
        }
    }

    private static Dictionary<string, int> BuildDomainMap(IList<Market> markets)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (domain, marketName) in DomainToMarketName)
        {
            var market = markets.FirstOrDefault(m =>
                m.Name.Equals(marketName, StringComparison.OrdinalIgnoreCase));
            if (market is not null)
                map[domain] = market.Id;
        }

        // Fallback: eşleşme yoksa BaseUrl'den domain türet
        foreach (var market in markets)
        {
            if (market.BaseUrl is null) continue;
            if (!Uri.TryCreate(market.BaseUrl, UriKind.Absolute, out var uri)) continue;
            var host = uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
            if (!map.ContainsKey(host))
                map[host] = market.Id;
        }

        return map;
    }

    private static int? FindMarketId(Dictionary<string, int> domainMap, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
        return domainMap.TryGetValue(host, out var id) ? id : null;
    }

    private static string StripQuery(string url)
    {
        try
        {
            var u = new Uri(url);
            // PHP-tabanlı siteler ?slug= veya ?id= ile ürünü tanımlar — query'yi koru
            if (u.AbsolutePath.EndsWith(".php", StringComparison.OrdinalIgnoreCase))
                return url;
            return new Uri(u.GetLeftPart(UriPartial.Path)).ToString().TrimEnd('/');
        }
        catch
        {
            return url;
        }
    }
}
