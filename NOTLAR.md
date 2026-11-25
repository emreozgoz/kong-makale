# Kong Gateway - Lokal Deneme Notlari

## Kong ne ise yariyor?

Kong bir **API Gateway** / reverse proxy. Client'larin ile backend servislerin arasina
girer ve her istek ondan gecer. Boylece her servise tek tek yazmak zorunda oldugun
"ortak isler"i tek yerden hallediyorsun:

- **Yonlendirme (routing):** path / host / header / method'a gore hangi backend'e gidecegini secer
- **Kimlik dogrulama:** API key, JWT, OAuth2, Basic auth... backend'in hic haberi olmaz
- **Rate limiting / quota:** dakikada N istek, musteri bazli kota
- **Load balancing & health check:** birden fazla backend arasinda dagitim, olen node'u devre disi birakma
- **Donusturme:** header/body/query ekleme-silme, request-response transform
- **Gozlemlenebilirlik:** log, metrik (Prometheus), tracing
- **Guvenlik:** CORS, IP kisitlama, bot tespiti, request size limit

Temel kavramlar:

| Kavram | Ne demek |
|---|---|
| **Service** | Arka taraftaki gercek servis (upstream). Ornek: `http://httpbin:8080` |
| **Route** | Gelen istegin hangi kurala uyarsa o Service'e gidecegi. Ornek: path `/demo` |
| **Plugin** | Istek/yanit uzerinde calisan davranis. Global, Service, Route veya Consumer bazli |
| **Consumer** | API'yi tuketen taraf (kullanici/uygulama). Anahtarlar ve kotalar buna baglanir |
| **Upstream** | Birden fazla hedefi (target) olan load-balanced havuz |

Akis: `Client -> [Route eslesir] -> [Plugin'ler calisir] -> Service -> Backend`

## Bu ortamda ne var?

| Adres | Ne |
|---|---|
| http://localhost:8000 | **Proxy** - istekleri buraya atiyorsun |
| http://localhost:8001 | **Admin API** - konfigurasyon REST API'si |
| http://localhost:8002 | **Kong Manager** - web arayuz (tarayicidan ac) |
| http://localhost:8080 | **httpbin** - test backend'i (Kong'u bypass edip direkt gorebilirsin) |

Hazir tanimlar:

- Service `httpbin-service` -> `http://httpbin:8080`
- Route `/demo`    -> plugin yok, serbest
- Route `/limited` -> `rate-limiting` 5 istek/dakika
- Route `/secure`  -> `key-auth`, gecerli anahtar: `gizli-anahtar-123`
- Consumer `emre`  -> yukaridaki anahtarin sahibi

## Hizli denemeler

```bash
# Duz proxy
curl -s localhost:8000/demo/get | jq

# Kong'un ekledigi header'lari gor
curl -i localhost:8000/demo/get

# Rate limit'i patlat (6. istek 429 doner)
for i in $(seq 1 7); do curl -s -o /dev/null -w "%{http_code}\n" localhost:8000/limited/get; done

# key-auth
curl -i localhost:8000/secure/get                              # 401
curl -i -H 'apikey: gizli-anahtar-123' localhost:8000/secure/get # 200

# Status kodlarini test et
curl -i localhost:8000/demo/status/500
curl -s localhost:8000/demo/delay/2    # yavas backend
```

## Admin API cheatsheet

```bash
# Listeleme
curl -s localhost:8001/services | jq '.data[].name'
curl -s localhost:8001/routes   | jq '.data[] | {name, paths}'
curl -s localhost:8001/plugins  | jq '.data[] | {name, route, service}'
curl -s localhost:8001/consumers | jq '.data[].username'

# Kurulabilen tum plugin'lerin listesi
curl -s localhost:8001/ | jq -r '.plugins.available_on_server | keys[]'

# Yeni service + route
curl -X POST localhost:8001/services --data name=my-api --data url=http://host.docker.internal:3000
curl -X POST localhost:8001/services/my-api/routes --data 'paths[]=/my' --data name=my-route

# Plugin ekle (ornekler)
curl -X POST localhost:8001/routes/demo-route/plugins --data name=cors
curl -X POST localhost:8001/routes/demo-route/plugins \
  --data name=request-transformer --data 'config.add.headers=X-Deneme:merhaba'
curl -X POST localhost:8001/plugins --data name=prometheus   # global

# Silme
curl -X DELETE localhost:8001/routes/demo-route
curl -X DELETE localhost:8001/plugins/<plugin-id>
```

> Not: kendi makinendeki bir servisi upstream yapmak icin container icinden
> `http://host.docker.internal:PORT` adresini kullan.

## Ortam yonetimi

```bash
cd ~/Desktop/kong-test
docker compose ps              # durum
docker compose logs -f kong    # canli log (her istek burada gorunur)
docker compose stop            # durdur (veri kalir)
docker compose up -d           # tekrar baslat
docker compose down            # sil (volume kalir)
docker compose down -v         # her seyi + DB'yi sil
```

## Dikkat

Admin API (8001) burada **kimlik dogrulamasiz** ve disa acik - sadece lokal deneme icin.
Gercek ortamda Admin API asla public olmaz.

---

# .NET Servisleri (services/)

Iki minimal API. Ikisinin de compose'da **`ports:` tanimi yok** - yani host'tan
dogrudan erisilemiyorlar, sadece Docker ic aginda yasiyorlar. Disariya tek kapi Kong.

## products-api

| Endpoint | Durum |
|---|---|
| `GET /products` | **PUBLIC** - Kong uzerinden disariya acildi |
| `GET /products/{id}` | internal (stok bilgisi iceriyor) |
| `GET /internal/stats` | internal (ciro/stok verisi, disariya cikmamali) |
| `GET /health` | internal |

## orders-api

| Endpoint | Durum |
|---|---|
| `GET /orders` | internal |
| `GET /orders/{id}` | internal |
| `POST /orders` | internal - products-api'yi **ic agdan** cagirir, Kong'a ugramaz |
| `GET /health` | internal |

## Disa acma konfigurasyonu

```bash
# Service: upstream adresi + path
curl -X POST localhost:8001/services \
  --data name=products-service \
  --data url=http://products-api:8080/products

# Route: SADECE GET, ve path TAM OLARAK /api/products
curl -X POST localhost:8001/services/products-service/routes \
  --data name=public-products \
  --data 'paths[]=~/api/products$' \
  --data 'methods[]=GET' \
  --data strip_path=true
```

**Kritik detay:** path'in basindaki `~` bunun regex oldugunu, sonundaki `$` ise
tam eslesme gerektigini soyler. Bunu yazmazsan (`paths[]=/api/products` gibi)
Kong **prefix** eslestirir ve `/api/products/3`, `/api/products/internal/...`
gibi her sey de acilir. Tek endpoint acacaksan `$` sart.

`methods[]=GET` de ayni mantikta: POST/PUT/DELETE istekleri route'a hic eslesmez.

## Dogrulanmis davranis

```
GET  /api/products        -> 200  (urun listesi doner)
GET  /api/products/3      -> 404  (route yok, servise hic gitmez)
GET  /api/internal/stats  -> 404
GET  /api/orders          -> 404
POST /api/products        -> 404  (method eslesmedi)
```

404'lerin onemi: istek .NET servisine **hic ulasmiyor**. Servis bu isteklerden
haberdar bile degil, Kong kapida cevirdi.

## Denemek icin

```bash
# Acik endpoint
curl -s localhost:8000/api/products | jq

# Ic aga girip servisleri dogrudan gor (host'tan erisim yok, container'dan var)
docker run --rm --network kong-test_default curlimages/curl -s http://products-api:8080/internal/stats
docker run --rm --network kong-test_default curlimages/curl -s http://orders-api:8080/orders
docker run --rm --network kong-test_default curlimages/curl -s -X POST http://orders-api:8080/orders \
  -H 'Content-Type: application/json' -d '{"productId":2,"quantity":3}'

# Kodu degistirdikten sonra
docker compose up -d --build products-api orders-api
```

## Sonraki adim fikirleri

```bash
# Public endpoint'e rate limit (public olan her sey limitlenmeli)
curl -X POST localhost:8001/routes/public-products/plugins \
  --data name=rate-limiting --data config.minute=60 --data config.policy=local

# Tarayicidan cagrilacaksa CORS
curl -X POST localhost:8001/routes/public-products/plugins --data name=cors

# Yanit cache'leme (ayni liste tekrar tekrar uretilmesin)
curl -X POST localhost:8001/routes/public-products/plugins \
  --data name=proxy-cache --data config.strategy=memory --data 'config.content_type=application/json'
```

---

# PostgreSQL'e disaridan erisim (DBeaver)

Host'ta zaten 5432'de baska bir PostgreSQL calistigi icin Kong'un DB'sini
**5433**'ten disari actik (`docker-compose.yml` -> `127.0.0.1:5433:5432`).

## DBeaver baglanti bilgileri

| Alan | Deger |
|---|---|
| Driver | PostgreSQL |
| Host | `localhost` |
| Port | **5433** |
| Database | `kong` |
| Username | `kong` |
| Password | `kongpass` |

## Terminalden

```bash
# Host'tan (psql kuruluysa)
PGPASSWORD=kongpass psql -h localhost -p 5433 -U kong -d kong

# Container'dan (psql kurulu olmasa da calisir)
docker compose exec kong-db psql -U kong -d kong
```

## Ilginc tablolar

| Tablo | Icerik |
|---|---|
| `services` | upstream tanimlari |
| `routes` | eslesme kurallari (`paths`, `methods`, `hosts`) |
| `plugins` | hangi plugin nerede acik, `config` kolonu JSONB |
| `consumers` | API tuketicileri |
| `keyauth_credentials` | API anahtarlari (**duz metin**, hash'li degil) |
| `ratelimiting_metrics` | rate limit sayaclari (policy=cluster/redis ise dolar) |
| `targets` / `upstreams` | load balancer havuzlari |

```sql
-- route + hangi service'e bagli
select r.name as route, r.paths, r.methods, s.name as service, s.host, s.path
from routes r join services s on r.service_id = s.id;

-- plugin konfigurasyonlari
select name, enabled, config from plugins;
```

## UYARI: buraya yazma

Kong konfigurasyonu **node baslarken bellege yuklenir** ve DB degisiklikleri
otomatik yayilmaz. SQL ile `insert`/`update` yaparsan Kong gormeyebilir veya
tutarsiz duruma duser. Okumak/incelemek icin serbest, degistirmek icin
**Admin API (8001) veya Kong Manager (8002)** kullan.

---

# "Kong kapida ceviriyor"u gozlemleme

Engellenen bir istek .NET servisine **hic ulasmiyor**. Bunu uc yerden gorebilirsin.

## 1) Canli karar akisi - ./izle.sh

```bash
./izle.sh          # ayri bir terminalde birak, sonra baska terminalden istek at
```

Ornek cikti:

```
200  GET   /api/products       -> GECTI    products-service/products  [route=public-products, upstream 4ms]
404  GET   /api/products/3     -> CEVRILDI  eslesen route yok, hicbir servise gitmedi
404  GET   /api/internal/stats -> CEVRILDI  eslesen route yok, hicbir servise gitmedi
404  POST  /api/products       -> CEVRILDI  eslesen route yok, hicbir servise gitmedi
```

Bu script global `file-log` plugin'inin yazdigi JSON'u okuyor:

```bash
curl -X POST localhost:8001/plugins \
  --data name=file-log --data config.path=/dev/stdout --data config.reopen=true
```

Plugin'in ham ciktisinda kritik alanlar:

| Alan | Izinli istek | Engelli istek |
|---|---|---|
| `route` | `public-products` | `null` |
| `service` | `products-service` | `null` |
| `upstream_uri` | `/products` | `""` |
| `latencies.proxy` | `4` (ms) | `-1` (upstream'e gidilmedi) |

`latencies.proxy = -1` tam olarak "arkaya hic gitmedik" demek.

## 2) Iki tarafin loglarini karsilastir

```bash
# Ayri terminalde ikisini birden izle
docker compose logs -f kong products-api
```

5 istek at (1 izinli + 4 engelli), sayilari karsilastir:

```bash
docker compose logs kong --tail 200 | grep -cE '"(GET|POST) /api'   # -> 5
docker compose logs products-api | grep -c 'Request starting'       # -> 1
```

Kong 5 gorur, .NET servisi 1. Aradaki 4 istek servise hic varmadi.

## 3) Yanit header'lari

```bash
curl -i localhost:8000/api/products        # izinli
curl -i localhost:8000/api/internal/stats  # engelli
```

| Header | Izinli | Engelli |
|---|---|---|
| `Server` | **`Kestrel`** (.NET cevapladi) | **`kong/3.9.4`** (Kong cevapladi) |
| `X-Kong-Upstream-Latency` | var | **yok** |

`Server` header'i en hizli ipucu: `Kestrel` goruyorsan istek arkaya gitti,
`kong/...` goruyorsan kapida cevrildi.

---

# Konfigurasyon nerede duruyor?

**.NET kodunda DEGIL.** `products-api` hangi endpoint'inin disa acik oldugunu bilmiyor.
`docker-compose.yml`'de de degil. Ayar Kong'un **Postgres veritabaninda** (`routes`
ve `services` tablolari). Oraya `curl` ile Admin API uzerinden yazildi.

Ayni veriye bakan uc arayuz:

| Yer | Nasil |
|---|---|
| Admin API | `curl localhost:8001/routes/public-products` |
| Kong Manager | http://localhost:8002 -> Routes -> public-products -> Edit |
| Veritabani | DBeaver (localhost:5433) -> `routes` tablosu (sadece OKU) |

Acik endpoint'i belirleyen iki alan:

```
paths   = ~/api/products$    <- hangi path (regex, $ = tam eslesme)
methods = GET                <- hangi method
```

Bunlari degistirirsen acik olan endpoint degisir. Ornek:

```bash
# Sadece path'i degistir
curl -X PATCH localhost:8001/routes/public-products --data 'paths[]=~/api/v2/urunler$'

# Method ekle
curl -X PATCH localhost:8001/routes/public-products --data 'methods[]=GET' --data 'methods[]=HEAD'
```

> Degisiklik birkac saniye icinde yayilir (`db_update_frequency`, default 5sn).
> Hemen test edersen eski davranisi gorebilirsin.

## Sorun: bu konfig hicbir dosyada yok

`docker compose down -v` dersen volume ile birlikte **tum route/service/plugin
tanimlari silinir** ve hepsini bastan curl'lemek gerekir. Konfig git'te de yok,
code review'dan da gecmiyor.

## Cozum: decK (declarative config)

Kong'un resmi CLI'i. Konfigurasyonu YAML dosyasina dokup geri yukleyebiliyor.

```bash
D="docker run --rm --network kong-test_default -v $PWD:/work -w /work kong/deck:latest"

# Mevcut konfigi dosyaya dok
$D gateway dump --kong-addr http://kong:8001 -o kong.yaml

# Dosya ile Kong arasindaki farki gor (uygulamadan once)
$D gateway diff --kong-addr http://kong:8001 kong.yaml

# Dosyayi uygula (Kong'u dosyadaki hale getirir)
$D gateway sync --kong-addr http://kong:8001 kong.yaml
```

`kong.yaml` artik bu klasorde. Sifirdan kurulumda:
`docker compose up -d` + `deck gateway sync` -> her sey geri gelir.

**Dikkat:** `kong.yaml` icinde `gizli-anahtar-123` **duz metin** duruyor.
Gercek projede API anahtarlari bu dosyaya girmemeli (decK'in
`--skip-consumers` flag'i veya env var / vault referanslari kullanilir).
