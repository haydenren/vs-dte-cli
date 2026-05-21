# Public Extraction Notes

Initial scope:

- Keep the generic Visual Studio DTE CLI command surface.
- Exclude internal artifacts, screenshots, business cases, and test data.
- Start from the current working-tree version of `tools/vs-dte-cli`.

Before publishing:

- Make `--solution`, `--test-dll`, and `--test` explicit or safely discovered.
- Replace internal defaults such as `SchoolSiteTest.sln` and
  `SchoolSiteScript.dll`.
- Replace `AUTOTEST_*` environment variable names with tool-owned names.
- Review all command help and examples for internal names.
- Add license after ownership approval.
