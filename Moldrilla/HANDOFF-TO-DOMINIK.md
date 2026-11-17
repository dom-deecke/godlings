# Moldrilla — handoff prompt for Dominik

Copy the following into a new coding chat opened in this repository:

---

Continue the Moldrilla website in `Moldrilla/`. Read its README and this handoff first. This website is separate from the Godlings Unity game in the repository root. Preserve the game and all its creative ownership rules; do not change its files or reuse its artwork.

## Run and inspect

From the repository root:

```sh
cd Moldrilla
python3 -m unittest test_pricing.py
python3 server.py
```

Open `http://127.0.0.1:4177` and `http://127.0.0.1:4177/admin`. Python 3.11 or newer is recommended; no third-party dependencies are required. The server listens only on loopback. Stop it with Ctrl+C. If port 4177 is occupied, identify the process before stopping anything.

## Product and design direction

Moldrilla is my custom-design, mold-making, 3D-printing, and small-production business for hobbyists and small companies. The approved palette is indigo `#455391`, periwinkle `#95A6DB`, cream `#E6E2D8`, gold `#E6BD37`, and green `#38663D`, with dark ink `#202945` for contrast.

Keep the playful workshop identity, large typography, muted gallery backgrounds, and matching shaded 3D concepts. The illustrations are code-generated example meshes, not scans or production-ready STL files. Ask me before changing creative direction or adding artwork. Do not generate AI images or reuse the Godlings game artwork.

Spanish is the default language. ES/EN controls appear at the upper right of the page and inside the project modal, remember the visitor's choice, and preserve form values when switching. `i18n.js` translates presentation text while keeping submitted values canonical. Preserve this behavior; direct bilingual templates may be preferable as the application grows.

## Existing visitor experience

The landing gallery shows figures/miniatures, candles, concrete planters, dice, chocolate, keychains, jewelry wax patterns, soap, and a custom-idea entry. Dragging a model rotates it; clicking a model or its arrow starts the funnel.

A second gallery covers open-face, peel-away flexible, sideways-separating multipart, multicavity, tooling to make silicone molds, and 3D printing. Mold-making tooling is distinct from the finished silicone production mold: it is a rigid casting box with a positive master. The printing category includes physical prototypes, printed masters, and sacrificial wax patterns for investment casting. A wax pattern is not finished metal jewelry.

Keep the guided six-step modal flow:

1. Deliverable: 3D design/STL, mold, 3D print, or finished pieces.
2. Starting point: idea, drawing/reference, existing 3D file, or physical object; description and optional reference upload.
3. Finished-object dimensions in mm, or help choosing a size.
4. Material/use; print jobs distinguish printing resin and castable wax.
5. Quantity, finish, preferred date, and delivery postal code.
6. Review and quote, followed by name/email and final submission.

Do not display prices, internal production hours, material costs, or payment prompts before step 6. Do not ask customers to estimate workshop labor. Back navigation preserves answers. Printing and unfamiliar processes require a reviewed quote rather than using casting-material rates.

## Pricing and administration

The local admin edits design cost/hour, setup/project, master/tooling material cost/cm³, silicone cost/kg and density, finished-material cost/cm³ (resin, wax, concrete, plaster), production labor/hour, waste %, gross margin %, and minimum project price. There are editable assumptions for design hours by complexity, labor per mold and piece, mold wall thickness, and master print fill.

Actual rates start blank. A blank rate must not silently become zero. Density 1.1 g/cm³, waste 10%, margin 40%, design hours, and production assumptions are placeholders to review, not measured or market-verified figures. Use my actual costs before quoting real customers.

Cost = design + setup + tooling + silicone + casting material + labor + finishing. Material consumption includes waste. Price = cost / (1 - gross margin), subject to the minimum; shipping and tax are excluded from the visitor estimate. Setup/design are project-level costs. Silicone kg = cm³ × density / 1000.

Gallery volumes use approximate shape fractions, not analysis of uploaded files. Unknown/custom geometry, missing rates, food/ice/soap processes, special finishes, larger casting batches, and prints go to review. Do not call those estimates confirmed production quotes.

## Files and persistence

- `index.html`: page and modal shell.
- `style.css`: responsive palette and layouts.
- `renderer.js`: local mesh generation and shaded canvas rendering.
- `app.js`: galleries, funnel, local project submission, and admin UI.
- `i18n.js`: Spanish/English presentation layer.
- `server.py`: local HTTP server, quote calculation, pricing persistence, and project/reference storage.
- `test_pricing.py`: eight pricing and estimation tests.

`server.py` embeds CSS and JS in the HTML response for reliable local preview loading. It serves `/api/pricing`, `/api/quote`, `/api/estimate`, and `/api/projects`. The local `admin` route shares the page shell.

Private runtime state lives under ignored `.data/`: `pricing.json`, project JSON files, and optional uploaded references. References support PNG/JPEG/WebP/PDF/STL/OBJ up to 8 MB. Submitting a brief saves it locally, shows a reference number, and lists it in admin. It does not email anyone. Do not commit `.data/`, personal submissions, uploads, secrets, or test artifacts.

## Before a real launch

The repository contains source, not a live production deployment. GitHub Pages cannot run the Python API. Do not deploy the local server unchanged: it has no admin authentication and is intentionally loopback-only. Agree on backend hosting and a durable private storage/authentication approach, preserve private pricing and submissions, and then implement it. Public visitors must not be able to read admin rates, customer briefs, or uploaded references.

Ask me for real rates, service capabilities, contact details, and the desired hosting/domain. Prioritize an authenticated admin, public/private API separation, production hosting and persistence, a working contact/notification path, and reviewed quote handling. Add actual geometry/printability analysis only when we agree on its scope. No payments are configured.

Validate meaningful changes with the pricing tests plus browser checks for ES/EN, selection of each deliverable, back/forward answer preservation, mobile modal overflow, reference handling, and the final quote/submission. Never claim a deployment or email delivery without verifying it.

Start by running and inspecting the current site, summarize its state, and ask me which improvement to tackle next.

---
