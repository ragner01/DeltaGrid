# Field Device Provisioning Guide

1) Enrollment
- Register device in MDM; install Field App and root/jailbreak detection helper.
- Issue per-device client certificate for gRPC mutual TLS.

2) First Run
- Device attestation sent to server (model, OS version, integrity signal).
- Receive assigned routes and allowed asset path prefixes.

3) Offline Setup
- Initialize SQLite database; cache twin subset for assigned routes.
- Download form schemas (WOs, PTW, readings) for offline rendering.

4) Security
- Secure storage for tokens and client cert; biometric unlock.
- Remote wipe: app polls wipe flag; upon set, clears SQLite, keys, and logs.

5) Sync
- Background sync with exponential backoff; vector-clock delta pull; batched push.
- Attachments sent via gRPC with binary chunks; resume on reconnect.

6) Testing
- Airplane-mode tests; conflict simulations; verify merge and user prompts.
