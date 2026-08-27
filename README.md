# carenova-user-auth-service

## Local admin account

For testing role-restricted features (Admin dashboard, user/role management) on your own machine, register through the app with:

- **Email:** `admin@carenova.dev`
- **Password:** `Admin@2026`

Then promote it to Admin once, in your local MySQL:

```sql
USE carenova_user_auth;
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@carenova.dev';
```

Every team member needs to do this on their own local database — there's no shared database between us yet, only once this is deployed to Azure.