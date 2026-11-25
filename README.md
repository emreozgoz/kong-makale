# kong-makale

Kong Gateway OSS + .NET 9 denemesi. Host'a hiç port açmayan iki minimal
API'nin yalnızca bir endpoint'i Kong üzerinden dışarıya açılıyor; engellenen isteklerin
.NET servisine hiç ulaşmadığı üç ayrı yöntemle gözlemleniyor.

Makalenin tamamı: [makale.md](makale.md)
Web sürümü: **https://emreozgoz.github.io/kong-makale/**

## Hızlı başlangıç

```bash
docker compose up -d

# Açık olan tek endpoint
curl -s localhost:8000/api/products | jq

# Gateway'in engelledikleri (istek .NET servisine hiç gitmez)
curl -s -o /dev/null -w "%{http_code}\n" localhost:8000/api/internal/stats   # 404
curl -s -o /dev/null -w "%{http_code}\n" localhost:8000/api/products/3       # 404

# Kong'un her istek icin verdigi karari canli izle
./izle.sh
```

Konfigürasyonu sıfırdan geri yüklemek için (decK):

```bash
docker run --rm --network kong-test_default -v $PWD:/work -w /work kong/deck:latest \
  gateway sync --kong-addr http://kong:8001 kong.yaml
```

## İçerik

| Dosya | Ne |
|---|---|
| `makale.md` | Yazının tamamı |
| `NOTLAR.md` | Deneme notları + Admin API cheatsheet |
| `docker-compose.yml` | Kong + Postgres + migrations + httpbin + .NET servisleri |
| `kong.yaml` | decK dump'ı (declarative config) |
| `izle.sh` | Kong'un karar akışını canlı gösteren script |
| `services/products-api` | .NET 9 minimal API — 1 public, 3 internal endpoint |
| `services/orders-api` | .NET 9 minimal API — tamamı internal |

## Adresler

| Adres | Ne |
|---|---|
| `localhost:8000` | Proxy — istekler buraya |
| `localhost:8001` | Admin API |
| `localhost:8002` | Kong Manager |
| `localhost:5433` | Postgres (DBeaver vb. için) |

> Uyarı: Bu kurulum lokal deneme içindir. Admin API (8001) authentication'sız ve dışa
> açık, `kong.yaml` içindeki API key düz metin. Gerçek ortamda ikisi de böyle olmaz.
