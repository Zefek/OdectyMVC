# OdectyStat – API reference

Read-only HTTP API mikroslužby pro odečty měřidel. Určeno pro konzumaci gateway API,
které sedí před touto službou a řeší autorizaci.

- **Base URL**: `http://127.0.0.1:5080` (konfigurovatelné v `appSettings.json` přes `Kestrel:Endpoints:Http:Url`)
- **Bind**: pouze loopback – nedostupné mimo server
- **Autentizace**: žádná (autorizaci dělá gateway)
- **Write path**: tato služba zápisové endpointy **nevystavuje**, zápisy chodí výhradně přes RabbitMQ

## Konvence

- Všechny odpovědi: `application/json; charset=utf-8`, kromě `lastphoto` (binární)
- Datum/čas: ISO 8601 bez timezone, **lokální čas serveru**
- Chyby: RFC 7807 `application/problem+json`

---

## `GET /api/gauges`

Seznam všech měřidel s posledním stavem.

### Response `200 OK`

```json
[
  {
    "id": 1,
    "description": "Vodoměr studna",
    "type": "Water",
    "lastValue": 123.4567,
    "lastMeasurementAt": "2026-05-18T08:15:00",
    "hasPhoto": true
  },
  {
    "id": 2,
    "description": "Elektroměr HDO",
    "type": "Electricity",
    "lastValue": 45678.0,
    "lastMeasurementAt": null,
    "hasPhoto": false
  }
]
```

| Pole | Typ | Popis |
|---|---|---|
| `id` | `int` | ID měřidla |
| `description` | `string` | Popis (`Gauge.Description`) |
| `type` | `string` | Typ měřidla (`Gauge.Type`, např. `"Water"`, `"Electricity"`) |
| `lastValue` | `decimal` | Poslední kumulativní stav |
| `lastMeasurementAt` | `DateTime?` | Čas posledního měření; `null` pokud měřidlo nikdy nezaznamenalo měření |
| `hasPhoto` | `bool` | `true` ⇒ volání `/lastphoto` má smysl; `false` ⇒ vrátí `404` |

### Statusy

- `200` – vždy (i prázdné pole, pokud žádná měřidla nejsou)

---

## `GET /api/gauges/{id}/lastphoto`

Binární obsah fotky z posledního měření daného měřidla.

### Path parametr

| Parametr | Typ | Popis |
|---|---|---|
| `id` | `int` | ID měřidla |

### Response `200 OK`

- `Content-Type`: `image/jpeg` / `image/png` / `image/*` podle přípony (detekováno `FileExtensionContentTypeProvider`); fallback `application/octet-stream`
- `Content-Disposition`: `attachment; filename="<původní jméno souboru>"`
- Tělo: surový obsah souboru

### Statusy

- `200` – fotka existuje a vrátila se
- `404` – buď neexistuje žádný `GaugeMeasurement` s vyplněným `ImagePath` pro toto měřidlo,
  nebo se soubor nepodařilo dohledat na disku (loguje se warning)
- `400` – `id` není validní int (handluje routing)

### Poznámky

Gateway by měla nejdřív zavolat `/api/gauges` a teprve když je `hasPhoto: true`,
volat `/lastphoto`. Šetří to round-trip přes 404.

Soubor se hledá pod cestou `RecognizedSuccessFolder/{id}/{yyyy-MM-dd}/{ImagePath}`,
kde datum se odvozuje z `MeasurementDateTime` ±1 den (kvůli rozdílu mezi
`MeasurementDateTime` v DB a creation-time původního souboru použitým při ukládání).

---

## `GET /health/live`

Liveness probe. Vrátí `200` vždy, dokud proces běží. Neprovádí žádné kontroly závislostí.

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0010973",
  "entries": {}
}
```

### Statusy

- `200` – `status: "Healthy"`
- `503` – `status: "Unhealthy"` (v praxi nikdy, dokud proces žije)

---

## `GET /health/ready`

Readiness probe. Zkontroluje, že jsou dostupné všechny závislosti potřebné pro obsluhu requestů.

### Kontrolované závislosti

| Entry | Co se kontroluje |
|---|---|
| `sqlserver-odecty` | SQL Server (connection string `Odecty`) |
| `postgres-homeassistant` | Postgres (connection string `HomeAssistant`) |
| `postgres-diagnostics` | Postgres (connection string `Diagnostics`) |
| `rabbitmq` | AMQP connection na RabbitMQ (z `OdectySettings`) |

### Response `200 OK`

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0096718",
  "entries": {
    "sqlserver-odecty": {
      "data": {},
      "description": null,
      "duration": "00:00:00.0028340",
      "status": "Healthy",
      "tags": ["ready"]
    },
    "rabbitmq": {
      "data": {},
      "description": null,
      "duration": "00:00:00.0021110",
      "status": "Healthy",
      "tags": ["ready"]
    }
  }
}
```

### Statusy

- `200` – všechny závislosti `Healthy`
- `503` – aspoň jedna `Unhealthy` nebo `Degraded`; v `entries` je vidět která

---

## Chybové odpovědi (RFC 7807)

Neočekávané chyby vrací `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7234#section-5.5.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "traceId": "00-abc...-01"
}
```

`traceId` koresponduje s OpenTelemetry trace ID, kterým si gateway může dohledat
distribuovaný trace v collectoru.

---

## Observability

- **OTLP gRPC exporter** (logs, traces, metrics) → cílí na collector běžící na localhost
- **Service name**: `OdectyStat`
- **Instrumentace**: ASP.NET Core, HttpClient, EF Core, .NET Runtime metriky
- Endpoint kolektoru řízený env var `OTEL_EXPORTER_OTLP_ENDPOINT` (default `http://localhost:4317`)
