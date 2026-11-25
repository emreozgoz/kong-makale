#!/usr/bin/env bash
# Kong'un her istek icin verdigi karari canli gosterir.
# Kullanim: ./izle.sh   (ayri bir terminalde birak, sonra istek at)
cd "$(dirname "$0")"
echo "Kong karar akisi izleniyor... (Ctrl+C ile cik)"
echo
docker compose logs -f --tail 0 kong 2>&1 | python3 -u -c '
import sys, json, re
for raw in sys.stdin:
    m = re.search(r"\{.*\}", raw)
    if not m:
        continue
    try:
        d = json.loads(m.group(0))
    except Exception:
        continue
    req = d.get("request") or {}
    if "uri" not in req:
        continue
    status = (d.get("response") or {}).get("status")
    route  = (d.get("route") or {}).get("name")
    svc    = (d.get("service") or {}).get("name")
    up     = d.get("upstream_uri") or ""
    lat    = (d.get("latencies") or {}).get("proxy", -1)
    head   = "{:<4} {:<5} {:<28}".format(status, req.get("method",""), req.get("uri",""))
    if route:
        print(head + " -> GECTI    {}{}  [route={}, upstream {}ms]".format(svc, up, route, lat))
    else:
        print(head + " -> CEVRILDI  eslesen route yok, hicbir servise gitmedi")
'
