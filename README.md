# KeycloakApi

This API demonstrates how to protect ASP.NET endpoints using Keycloak and how to authenticate directly through Swagger UI using the password grant.

## Running

```bash
# restore dependencies and run the application
# Keycloak should be running locally and a client named `swagger-ui` must exist
# with direct access grants enabled.

 dotnet run --project KeycloakApi.csproj
```

Navigate to `http://localhost:5173/swagger` and use the **Authorize** button. Enter your Keycloak username and password to obtain a token for API calls.
