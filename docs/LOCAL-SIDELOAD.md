# Local sideload install (fix 0x800B010A)

If you try to install the MSIX/MSIXBundle and see this error:

> This app package’s publisher certificate could not be verified ... (0x800B010A)

Windows can’t build a trust chain for the signing certificate (it’s self‑signed). Trust the certificate once and the install will work.

## Option A — Install cert from the bundle (simple)

Run PowerShell as Administrator in the repo folder and install the signer cert directly from the bundle:

```powershell
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/install-cert-from-package.ps1 -PackagePath ./ViewMD.msixbundle
```

Retry the install by double‑clicking `ViewMD.msixbundle`.

## Option B — Install from the PFX you used to sign

If you have `./certs/ViewMD.pfx`:

```powershell
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/install-dev-certificate.ps1 -PfxPath ./certs/ViewMD.pfx
# You'll be prompted for the password securely (press Enter if none)
```

This puts the public cert into both Trusted Root and Trusted People (LocalMachine). If you can’t run as admin, add `-Scope CurrentUser` and re‑try the install.

## Optional: Enable Developer Mode

On some systems, enabling Developer Mode helps with sideloading:

Settings → System → For developers → Developer Mode (On)

You should still trust the certificate; Developer Mode is not a substitute for a valid chain.

## Clean up (remove trust)

```powershell
# If you know the thumbprint
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/uninstall-dev-certificate.ps1 -Thumbprint <THUMBPRINT>

# Or derive from a .cer
pwsh -ExecutionPolicy Bypass -File ./scripts/advanced/uninstall-dev-certificate.ps1 -CerPath ./certs/ViewMD.cer
```

---

For Microsoft Store users: none of this is needed; the Store re‑signs with a trusted certificate.
