import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine, isMainModule } from '@angular/ssr/node';
import express from 'express';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import bootstrap from './main.server';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');
const indexHtml = join(serverDistFolder, 'index.server.html');

const app = express();
// Angular validates the request Host header against this list before rendering, to prevent
// cache-poisoning via a spoofed Host — silently falls back to CSR otherwise. Override with
// NG_ALLOWED_HOSTS (comma-separated) in Docker/Azure where the real host isn't "localhost".
const commonEngine = new CommonEngine({ allowedHosts: ['localhost', '127.0.0.1'] });

/**
 * Serve static files from /browser — this is what actually serves the prerendered
 * login/register routes (app.routes.server.ts), since they exist as real index.html files here.
 */
app.get(
  '**',
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: 'index.html'
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 *
 * Note: this scaffold's CommonEngine always performs a full SSR bootstrap here, regardless of
 * app.routes.server.ts's RenderMode for a given path (that per-route enforcement is a newer
 * @angular/ssr runtime API — AngularNodeAppEngine — that needs a build-manifest shape this CLI
 * version's `ng add @angular/ssr` scaffold doesn't produce; swapping it in throws "Angular app
 * engine manifest is not set" at server startup, confirmed by testing it directly).
 * app.routes.server.ts's Prerender/Client entries still matter for the *build*: `Prerendered N
 * static routes` at build time is what actually produces login/register as static files above.
 * For account/cart/orders (behind authGuard, RenderMode.Client): the server has no access to the
 * browser's localStorage-held JWT, so it renders the logged-out redirect-to-login state; the
 * client then hydrates, re-evaluates the guard against the real session, and navigates to the
 * actual destination if authenticated. A brief flash is possible on a slow connection — a known,
 * accepted trade-off of localStorage-based auth + SSR, not a bug.
 */
app.get('**', (req, res, next) => {
  const { protocol, originalUrl, baseUrl, headers } = req;

  commonEngine
    .render({
      bootstrap,
      documentFilePath: indexHtml,
      url: `${protocol}://${headers.host}${originalUrl}`,
      publicPath: browserDistFolder,
      providers: [{ provide: APP_BASE_HREF, useValue: baseUrl }],
    })
    .then((html) => res.send(html))
    .catch((err) => next(err));
});

/**
 * Start the server if this module is the main entry point.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

export default app;
