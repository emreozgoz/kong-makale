# Medium'a yapıştırma

`makale-medium.html` dosyasını **tarayıcıda aç** (çift tıkla), sonra:

1. `Cmd+A` → `Cmd+C`
2. Medium'da yeni story aç, `Cmd+V`

Markdown dosyasını (`makale.md`) yapıştırma — Medium markdown'ı metin olarak alır,
`##` ve `**` işaretleri ekranda kalır. Rich text (tarayıcıdan kopyalanmış HTML)
yapıştırdığında başlıklar, kalın/italik, listeler, linkler, alıntılar ve kod
blokları olduğu gibi gelir.

## Yapıştırdıktan sonra

- **Başlık/alt başlık:** Medium ilk `H1`'i story başlığı, ilk `H2`'yi alt başlık yapar.
  Yani "1. Giriş: Neden API Gateway?" alt başlık olarak görünür — Camunda yazısındaki
  düzenin aynısı.
- **Kod blokları:** Her biri tek bir code block olarak gelir. Medium'da syntax
  highlight istersen bloğun sol üstündeki üç noktadan dil seçebilirsin.
- **Görseller:** 16 görsel dosyada hazır, kaynakları GitHub raw adresleri
  (`raw.githubusercontent.com/emreozgoz/kong-makale/main/images/...`). Yapıştırdığında
  Medium bunları kendisi indirip story'ye yükler — elle eklemen gerekmez. Alt yazılar
  da `figcaption` olarak geliyor.
  Not: Bu yüzden repo public kalmalı; private yaparsan Medium görselleri çekemez.
- **Tablolar:** Medium tablo desteklemiyor. Bu dosyada tablolar madde listesine
  çevrildi, yani yapıştırınca hiçbir şey kaybolmuyor. Tablo görünümünü istersen
  `docs/index.html`'deki tabloların ekran görüntüsünü alıp görsel olarak ekle
  (Camunda yazısındaki `tablo-01` yöntemi).

## Etiketler

`kong`, `api-gateway`, `dotnet`, `docker`, `microservices`
