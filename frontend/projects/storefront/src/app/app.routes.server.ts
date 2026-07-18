import { RenderMode, ServerRoute } from '@angular/ssr';

// Public, SEO-relevant routes are server-rendered or prerendered; anything behind authGuard
// carries no SEO value and depends on client-only auth state (see AuthService), so it stays CSR.
export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Server },
  { path: 'login', renderMode: RenderMode.Prerender },
  { path: 'register', renderMode: RenderMode.Prerender },
  { path: '**', renderMode: RenderMode.Client }
];
