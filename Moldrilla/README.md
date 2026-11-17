# Moldrilla local website

Run `python3 server.py`, then open http://127.0.0.1:4177.
Pricing administration: http://127.0.0.1:4177/admin.

Settings persist in `.data/pricing.json`. No dependencies are required.
Run `python3 -m unittest test_pricing.py` to check the pricing calculations.

Actual cost rates start blank. Waste allowance (10%), silicone density (1.1 g/cm³),
and gross margin (40%) are editable starting assumptions, not researched costs.
The server blocks quotes that need unconfigured rates.

The landing page includes eight locally rendered, rotatable 3D concepts plus a
custom-project entry, plus open-face, flexible peel-away, multipart, multicavity, silicone-mold tooling, and 3D-printing concepts. Each opens a six-step modal: deliverable (design, mold, 3D print, or finished pieces), starting point,
dimensions, material, quantities and finishing, then review and quote. No prices
or internal production costs are displayed before the final step.

Project briefs and optional reference files are stored in `.data/projects/` and
listed in the admin panel. Supported references: PNG, JPEG, WebP, PDF, STL, OBJ,
up to 8 MB. Submissions stay local; email notifications are not configured.

Preliminary estimates use gallery-specific approximate volume fractions and the
editable design-hour, labor, mold-wall, and master-fill assumptions. This is not
geometry analysis of uploaded models. Missing rates, custom projects, unknown
dimensions, food/ice/soap processes, special finishes, and larger casting batches
require review. Shipping and tax are excluded. Hosted admin authentication is not
implemented; the development server binds only to 127.0.0.1.

Jewelry selections default to a physical 3D print and offer castable wax for investment-casting patterns. Prints require review; print jobs are never priced using casting-material rates.

Spanish is the default. The top-right ES/EN toggle and modal toggle remember the selected language while preserving form values.

See `HANDOFF-TO-DOMINIK.md` for the continuation prompt and launch requirements. This folder contains source for a local server, not a GitHub Pages deployment.
