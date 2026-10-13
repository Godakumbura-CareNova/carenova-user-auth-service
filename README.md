# carenova-user-auth-service

## GCN-47 assigned-patient contacts

POST `/api/doctor/patient-contacts` accepts `{ "patientIds": ["patient-guid"] }` for an authenticated Doctor. Batches must contain 1–100 unique, nonempty GUIDs. Before querying contacts, auth forwards the caller's JWT to the appointment service's database-only ownership check. Only assigned patients' `patientId`, `fullName` and `mobileNumber` are returned. Failed or malformed ownership verification returns 503 without contacts; unassigned IDs are omitted.

Set `AppointmentIntegration__BaseUrl` to the **direct appointment service origin**, locally `http://localhost:5136`. The appointment service needs `UserAuthService__BaseUrl` pointing to this service. Both services must validate the same JWT issuer, audience and signing key. Configure secrets privately through user secrets or deployment settings.

The lookup uses its own doctor-only controller. Existing own-profile and admin-directory permissions are unchanged. No auth database migration is needed. Run `dotnet test UserAuth.sln --configuration Release` for regression tests.
