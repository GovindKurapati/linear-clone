import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Functional interceptor (modern Angular). Registered via provideHttpClient(
// withInterceptors([authInterceptor])) in app.config.ts.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();

  // Attach the bearer token to every request that has one. Auth endpoints
  // (login/register) run before a token exists, so this simply no-ops there.
  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // A 401 means the token is missing/expired/invalid — log out and bounce
      // to login. (Skip this on the login call itself so a bad-password 401
      // doesn't trigger a redirect loop.)
      if (err.status === 401 && !req.url.includes('/auth/')) {
        auth.logout();
      }
      return throwError(() => err);
    }),
  );
};
