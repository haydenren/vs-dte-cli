# Public Extraction Notes

Initial scope:

- Keep the generic Visual Studio DTE CLI command surface.
- Exclude internal artifacts, screenshots, business cases, and test data.
- Start from the current working-tree version of `tools/vs-dte-cli`.

Before publishing:

- Confirm the public command contract for `--solution`, `--test-dll`, and
  `--test`.
- Review all command help and examples for product- or company-specific names.
- Add license after ownership approval.

Completed in this extraction workspace:

- Removed product-specific default solution and test DLL values.
- Replaced internal live-debug environment variable names with tool-owned
  `VSDTECLI_*` names.
- Removed dependency on an internal compatibility wrapper from preflight.
