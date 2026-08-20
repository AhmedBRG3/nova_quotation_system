# NOVA Quotation System

ASP.NET Core 9 (Razor Pages) + PostgreSQL 18 + EF Core. Quotations are entered through a web
form, stored as rows in Postgres, previewed as an A4 sheet, and downloaded as PDF.

## Stack

| Piece | Choice |
|---|---|
| Web | ASP.NET Core 9 Razor Pages |
| Database | PostgreSQL 18 via Npgsql + EF Core 9 |
| PDF | PuppeteerSharp — headless Chrome prints the same page you preview |
| Money | Computed, never stored: totals always derive from the item rows |

## Schema

- **`quotations`** — `job_no` (unique), `subject_en/ar`, `quote_date`, `company_en/ar`,
  `contact_person_en/ar`, `contact_details`, `vat_percent`, `terms`, `note_en/ar`, timestamps
- **`quotation_items`** — `quotation_id`, `sort_order`, `description_en/ar`, `quantity`, `unit_price`
- **`quotation_images`** — `quotation_id`, `sort_order`, `path`, `caption`
- **`company_profile`** — single row: agency details, logo path, default VAT %, default terms

Line totals, subtotal, VAT and grand total are calculated in code so stored rows can never
disagree with the printed sheet.

### Terms format

Terms are one clause per line. Text after a `|` becomes the Arabic column on the printed sheet:

```
This quotation is valid for 30 days. | هذا العرض صالح لمدة ٣٠ يوماً.
```

## First run

PostgreSQL 18 is installed at `/Library/PostgreSQL/18` and runs as a launch daemon, so the
server should already be up. Nothing to start.

**1. Set the connection string with your Postgres password.** Keep it out of git with
user-secrets:

<!-- ```bash
cd src/Inova.Quotations && dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=inova_quotations;Username=postgres;Password=YOUR_PASSWORD"
``` -->

**2. Run:**

```bash
dotnet run --project src/Inova.Quotations
```

The `inova_quotations` database is created on first start, migrations apply automatically,
and the company profile seeds itself with the default terms. On the first PDF export,
PuppeteerSharp downloads a private Chrome build (~150 MB, one time only).

## Routes

| Route | What |
|---|---|
| `/` | Quotation list, with search over job no., subject and company |
| `/Quotations/Edit` | New quotation (job number auto-suggested) |
| `/Quotations/Edit/{id}` | Edit |
| `/Quotations/Print/{id}` | A4 preview in the browser |
| `/quotations/{id}/pdf` | PDF download |
| `/Settings` | Agency details, logo upload, default VAT and terms |

## Layout reference

`templates/quotation.html` is the original static design with sample data — handy for
adjusting the layout without running the app. The live version of that CSS is
`src/Inova.Quotations/wwwroot/css/quotation.css`; edit that one to change the real output.

## Adding migrations

```bash
dotnet dotnet-ef migrations add SomeChange --project src/Inova.Quotations
```
